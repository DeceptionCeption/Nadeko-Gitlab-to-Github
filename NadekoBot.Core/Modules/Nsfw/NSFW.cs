using Discord;
using Discord.Commands;
using Nadeko.Microservices;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.NSFW
{
    [RequireNsfw]
    public class NSFW : NadekoTopLevelModule<SearchesService>
    {
        private new readonly SearchImages.SearchImagesClient _service;

        public NSFW(SearchImages.SearchImagesClient service)
        {
            _service = service;
        }

        public static Task NsfwReply(ICommandContext ctx, UrlReply data)
        {
            if (!string.IsNullOrWhiteSpace(data.Error))
            {
                return ctx.Channel.SendErrorAsync(data.Error);
            }

            return ctx.Channel.EmbedAsync(new EmbedBuilder()
                .WithOkColor()
                .WithImageUrl(data.Url)
                .WithDescription($"[link]({data.Url})")
                .WithFooter($"{data.Rating} ({data.Provider}) | {string.Join(" | ", data.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Take(3))}"));
        }

        public static TagRequest GetTagRequest(ICommandContext ctx, IEnumerable<string> tags = null, bool forceExplicit = false)
        {
            ulong guildId = ctx.Guild?.Id ?? 0;

            var tagRequest = new TagRequest
            {
                ForceExplicit = forceExplicit,
                GuildId = guildId,
            };

            if (!(tags is null))
                tagRequest.Tags.AddRange(tags);

            return tagRequest;
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Boobs()
        {
            var data = await Rpc(Context, _service.BoobsAsync, new BoobsRequest { });

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Butts()
        {
            var data = await Rpc(Context, _service.ButtsAsync, new ButtsRequest { });

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DanBooru(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.DanbooruAsync, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DerpiBooru(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.DerpiBooruAsync, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task GelBooru(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.GelbooruAsync, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task E621(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.E621Async, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Konachan(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.KonachanAsync, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Yandere(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.YandereAsync, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rule34(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags);

            var data = await Rpc(Context, _service.Rule34Async, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hentai(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags, forceExplicit: true);

            var data = await Rpc(Context, _service.HentaiAsync, payload);

            await NsfwReply(Context, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task HentaiBomb(params string[] tags)
        {
            var payload = GetTagRequest(Context, tags, forceExplicit: true);

            var images = await Task.WhenAll(ErrorlessRpc(Context, _service.GelbooruAsync, payload),
                                            ErrorlessRpc(Context, _service.DanbooruAsync, payload),
                                            ErrorlessRpc(Context, _service.KonachanAsync, payload),
                                            ErrorlessRpc(Context, _service.YandereAsync, payload)).ConfigureAwait(false);

            var linksEnum = images?.Where(data => data != null && string.IsNullOrWhiteSpace(data.Error)).ToArray();
            if (images == null || !linksEnum.Any())
            {
                await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
                return;
            }

            await Context.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.Url))).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageMessages)]
        public async Task NsfwTagBlacklist([Leftover] string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var reply = await Rpc(Context, _service.GetBlacklsitedTagsAsync, new GetBlacklistedTagsMessage
                {
                    GuildId = Context.Guild?.Id ?? 0,
                });

                await Context.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("blacklisted_tag_list"))
                    .WithDescription(reply.BlacklistedTags.Any()
                    ? string.Join(", ", reply.BlacklistedTags)
                    : "-")).ConfigureAwait(false);
            }
            else
            {
                tag = tag.Trim().ToLowerInvariant();
                var reply = await Rpc(Context, _service.ToggleBlacklistTagAsync, new BlacklistTagMessage
                {
                    Tag = tag,
                    GuildId = Context.Guild?.Id ?? 0,
                });

                if (reply.IsAdded)
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_add", tag).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_remove", tag).ConfigureAwait(false);
            }

        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task NsfwCc()
        {
            await Rpc(Context, _service.ClearCacheAsync, new ClearCacheRequest { });

            await Context.OkAsync();
        }
    }
}