using Discord;
using Discord.Commands;
using Nadeko.Microservices;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Nsfw
{
    public partial class Nsfw
    {
        [Group]
        [RequireNsfw]
        public class ManageNsfw : NadekoSubmodule
        {
            private readonly SearchImages.SearchImagesClient _service;

            public ManageNsfw(SearchImages.SearchImagesClient service)
            {
                _isNew = true;
                _service = service;
            }

            [NadekoCommand]
            [UserPerm(GuildPerm.ManageMessages)]
            public async Task NsfwTagBl(string tag = null)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    var reply = await Rpc(ctx, _service.GetBlacklsitedTagsAsync, new GetBlacklistedTagsMessage
                    {
                        GuildId = ctx.Guild?.Id ?? 0
                    });

                    await ctx.Channel.SendAsync(new EmbedBuilder()
                        .WithOkColor(ctx)
                        .WithTitle(GetText("blacklisted_tag_list"))
                        .WithDescription(reply.BlacklistedTags.Any()
                        ? string.Join(", ", reply.BlacklistedTags)
                        : "-")).ConfigureAwait(false);
                }
                else
                {
                    tag = tag.Trim().ToLowerInvariant();
                    var reply = await Rpc(ctx, _service.ToggleBlacklistTagAsync, new BlacklistTagMessage
                    {
                        Tag = tag,
                        GuildId = ctx.Guild?.Id ?? 0
                    });

                    if (reply.IsAdded)
                        await ReplyConfirmLocalizedAsync("blacklisted_tag_add", tag).ConfigureAwait(false);
                    else
                        await ReplyConfirmLocalizedAsync("blacklisted_tag_remove", tag).ConfigureAwait(false);
                }

            }

            [NadekoCommand]
            [OwnerOnly]
            public async Task NsfwCc()
            {
                await Rpc(ctx, _service.ClearCacheAsync, new ClearCacheRequest { });

                await ctx.OkAsync();
            }
        }
    }
}
