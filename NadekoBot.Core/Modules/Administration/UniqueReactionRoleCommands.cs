using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using NadekoBot.Modules;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Core.Modules.Administration
{
    public class UniqueReroService : INService
    {
        public class UreroState
        {
            public string EmoteName { get; set; }
            public ulong MessageId { get; set; }
            public ulong RoleId { get; set; }
            public Dictionary<ulong, List<ulong>> AppliedUsers { get; set; }
        }

        private Dictionary<ulong, UreroState> _ureroData = new Dictionary<ulong, UreroState>();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public UniqueReroService(DiscordSocketClient client)
        {
            if (!File.Exists("data/urero.json"))
                Save();

            var strData = File.ReadAllText("data/urero.json");
            _ureroData = JsonConvert.DeserializeObject<Dictionary<ulong, UreroState>>(strData);

            client.ReactionAdded += Client_ReactionAdded;
            client.ReactionRemoved += Client_ReactionRemoved;
        }

        private Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var _ = Task.Run(async () =>
            {
                if (!(channel is ITextChannel ch))
                    return;

                await semaphore.WaitAsync();
                try
                {
                    // check if this guild has enabled urero
                    if (!_ureroData.TryGetValue(ch.GuildId, out var settings))
                        return;

                    // check if its the same message
                    if (msg.Id != settings.MessageId)
                        return;

                    // emote has to be the same
                    if (settings.EmoteName != reaction.Emote.Name
                        && settings.EmoteName != reaction.Emote.ToString())
                        return;

                    // user must be applied previously
                    if (!settings.AppliedUsers.ContainsKey(reaction.UserId))
                        return;

                    var toAdd = settings.AppliedUsers[reaction.UserId];

                    var guild = ((SocketGuild)ch.Guild);
                    var user = guild.GetUser(reaction.UserId);
                    var role = guild.GetRole(settings.RoleId);


                    await user.RemoveRoleAsync(role, new RequestOptions
                    {
                        RetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502
                    });
                    await user.AddRolesAsync(toAdd.Select(roleId => guild.GetRole(roleId)).Where(role => role != null), new RequestOptions
                    {
                        RetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502
                    });
                    settings.AppliedUsers.Remove(reaction.UserId);
                }
                finally
                {
                    Save();
                    semaphore.Release();
                }
            });

            return Task.CompletedTask;
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msg,
            ISocketMessageChannel channel, SocketReaction reaction)
        {
            var _ = Task.Run(async () =>
            {
                if (!(channel is ITextChannel ch))
                    return;

                await semaphore.WaitAsync();
                try
                {
                    // check if this guild has enabled urero
                    if (!_ureroData.TryGetValue(ch.GuildId, out var settings))
                        return;

                    // check if its the same message
                    if (msg.Id != settings.MessageId)
                        return;

                    // emote has to be the same
                    if (settings.EmoteName != reaction.Emote.Name
                        && settings.EmoteName != reaction.Emote.ToString())
                        return;

                    // user shouldn't have been applied already
                    if (settings.AppliedUsers.ContainsKey(reaction.UserId))
                        return;

                    var guild = ((SocketGuild)ch.Guild);
                    var role = guild.GetRole(settings.RoleId);
                    var user = guild.GetUser(reaction.UserId);

                    var removedRoles = user.Roles.Where(x => !x.IsEveryone && !x.IsManaged);
                    await user.RemoveRolesAsync(removedRoles, new RequestOptions
                    {
                        RetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502
                    });
                    settings.AppliedUsers[reaction.UserId] = removedRoles.Select(x => x.Id).ToList();

                    await user.AddRoleAsync(role, new RequestOptions
                    {
                        RetryMode = RetryMode.RetryRatelimit | RetryMode.Retry502
                    });
                }
                finally
                {
                    Save();
                    semaphore.Release();
                }
            });

            return Task.CompletedTask;
        }

        public async Task<bool> RemoveAsync(ulong guildId)
        {
            await semaphore.WaitAsync();
            try
            {
                if (_ureroData.Remove(guildId))
                {
                    return true;
                }

                return false;
            }
            finally
            {
                Save();
                semaphore.Release();
            }
        }

        public async Task<bool> AddAsync(SocketGuild guild, IMessage message, string emoji, IRole role)
        {
            await semaphore.WaitAsync();
            try
            {
                if (_ureroData.ContainsKey(guild.Id))
                    return false;

                await message.AddReactionAsync(emoji.ToIEmote());

                _ureroData[guild.Id] = new UreroState
                {
                    AppliedUsers = new Dictionary<ulong, List<ulong>>(),
                    EmoteName = emoji,
                    MessageId = message.Id,
                    RoleId = role.Id
                };
                return true;
            }
            finally
            {
                Save();
                semaphore.Release();
            }
        }

        private void Save()
        {
            File.WriteAllText("data/urero.json", JsonConvert.SerializeObject(_ureroData, Formatting.Indented));
        }
    }

    public partial class Administration
    {

        public class UniqueReactionRoleCommands : NadekoSubmodule<UniqueReroService>
        {

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(GuildPerm.ManageRoles)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task UniqueReaction(string emoji, [Leftover] IRole role)
            {
                var msgs = await ((SocketTextChannel)ctx.Channel).GetMessagesAsync(ctx.Message.Id, Direction.Before, limit: 1).FlattenAsync().ConfigureAwait(false);
                var prev = msgs.FirstOrDefault();
                if (prev == null)
                {
                    await ctx.Channel.SendErrorAsync("message not found").ConfigureAwait(false);
                    return;
                }

                var res = await _service.AddAsync((SocketGuild)ctx.Guild, prev, emoji, role);
                if (res)
                {
                    await ctx.Channel.SendConfirmAsync("Unique reaction role started.").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendErrorAsync("This feature is already enabled on this server.").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [BotPerm(GuildPerm.ManageRoles)]
            [UserPerm(GuildPerm.ManageRoles)]
            public async Task UniqueReactionRm()
            {
                var res = await _service.RemoveAsync(ctx.Guild.Id);
                if (res)
                {
                    await ctx.Channel.SendConfirmAsync("Unique reaction role stopped.").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendErrorAsync("This feature isn't enabled on this server.").ConfigureAwait(false);
                }
            }
        }
    }
}
