using Ayu.Discord.Common;
using Discord;
using Discord.Commands;
using Nadeko.Microservices;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Nsfw
{
    [RequireNsfw]
    public partial class Nsfw : NadekoTopLevelModule
    {
        public Nsfw()
        {
            _isNew = true;
        }

        public static Task NsfwReply(ICommandContext ctx, UrlReply data)
        {
            if (!string.IsNullOrWhiteSpace(data.Error))
            {
                return ctx.Channel.SendErrorAsync(data.Error);
            }

            return ctx.Channel.SendAsync(embed: new EmbedBuilder()
                .WithOkColor(ctx)
                .WithImageUrl(data.Url)
                .WithDescription($"[link]({data.Url})")
                .WithFooter($"{data.Rating} ({data.Provider}) | {string.Join(" | ", data.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Take(3))}"));
        }
    }
}
