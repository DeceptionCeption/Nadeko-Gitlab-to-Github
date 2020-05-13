using Ayu.Discord.Common;
using Discord;
using Discord.WebSocket;
using Nadeko.Microservices;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using NadekoBot.Modules.Permissions.Common;
using NadekoBot.Modules.Permissions.Services;
using NLog;
using System.Threading.Tasks;

namespace NadekoBot.Modules.CustomReactions.Services
{
    public class ExpressionsService : IEarlyBehavior, INService
    {
        public int Priority => -1;
        public ModuleBehaviorType BehaviorType => ModuleBehaviorType.Executor;


        private readonly Logger _log;

        private readonly Nadeko.Microservices.Expressions.ExpressionsClient _expr;
        private readonly DiscordSocketClient _client;
        private readonly ReplacementBuilderService _repService;
        private readonly PermissionService _permService;
        private readonly Core.Services.ILocalization _oldLocalization;
        private readonly Ayu.Common.ILocalization _newLocalization;
        private readonly CommandHandler _cmd;

        private readonly GlobalPermissionService _gperm;

        public ExpressionsService(Nadeko.Microservices.Expressions.ExpressionsClient expr,
            DiscordSocketClient client, ReplacementBuilderService repService, PermissionService permService,
            Core.Services.ILocalization localization, Ayu.Common.ILocalization newLoc,
            GlobalPermissionService gperm, CommandHandler cmd)
        {
            _log = LogManager.GetCurrentClassLogger();
            _expr = expr;
            _client = client;
            _repService = repService;
            _permService = permService;
            _oldLocalization = localization;
            _gperm = gperm;
            _newLocalization = newLoc;
            _cmd = cmd;
        }

        public async Task<bool> RunBehavior(DiscordSocketClient client, IGuild guild, IUserMessage msg)
        {
            if (!(msg is SocketUserMessage message) || !(msg.Channel is SocketTextChannel channel))
            {
                return false;
            }

            // check global perms
            // if actualcustomreactions are globally blocked, just skip checking for them
            // i don't want to block because there might be a command with the same name
            if (_gperm.BlockedModules.Contains(Nadeko.Bot.Modules.Expressions.Expressions.ActualExpressionModuleName.ToLowerInvariant()))
            {
                return false;
            }

            var response = await _expr.QueryForExpressionAsync(new QueryForExpressionRequest
            {
                Content = message.Content,
                GuildId = channel.Guild.Id,
                BotId = client.CurrentUser.Id
            });

            // if there is no custom reaction - good

            if (response.ResCase != QueryForExpressionReply.ResOneofCase.Data)
                return false;


            var data = response.Data;

            var pc = _permService.GetCacheFor(channel.Guild.Id);
            if (!pc.Permissions.CheckPermissions(message, data.Trigger, Nadeko.Bot.Modules.Expressions.Expressions.ActualExpressionModuleName, out var index))
            {
                if (pc.Verbose)
                {
                    // todo migrate localization to v3
                    var locale = new Nadeko.Common.Localization.Locale(_newLocalization, _oldLocalization.GetCultureInfo(guild));
                    var cmd = pc.Permissions[index].GetCommand(_cmd.GetPrefix(channel.Guild), channel.Guild);
                    var returnMsg = locale.GetText("perm_prevent", Format.Bold((index + 1).ToString()), Format.Code(cmd));
                    try
                    {
                        await channel.SendAsync(new EmbedBuilder()
                            .WithErrorColor()
                            .WithDescription(returnMsg)).ConfigureAwait(false);
                    }
                    catch { }
                }
                return true;
            }

            var rep = _repService.CreateReplacementBuilder()
                .WithDefault(message.Author, channel, channel.Guild, _client)
                .Build();


            if (data.AutoDelete)
            {
                try { await message.DeleteAsync(); } catch { }
            }

            var toSend = SmartText.CreateFrom(data.Response);
            toSend = await rep.ReplaceAsync(toSend);

            if (data.DmResponse)
            {
                await message.Author.SendAsync(toSend);
            }
            else
            {
                await channel.SendAsync(toSend);
            }

            return true;
        }

    }
}
