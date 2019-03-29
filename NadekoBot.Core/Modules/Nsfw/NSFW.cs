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
        private readonly SearchImages.SearchImagesClient _service;

        public NSFW(SearchImages.SearchImagesClient service)
        {
            _service = service;
        }

        private Task NsfwReply(ICommandContext ctx, UrlReply data)
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

        private TagRequest GetTagRequest(ICommandContext ctx, IEnumerable<string> tags = null, bool forceExplicit = false)
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
        public async Task Boobs(ICommandContext ctx)
        {
            var data = await Rpc(ctx, _service.BoobsAsync, new BoobsRequest { });

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Butts(ICommandContext ctx)
        {
            var data = await Rpc(ctx, _service.ButtsAsync, new ButtsRequest { });

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DanBooru(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.DanbooruAsync, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task DerpiBooru(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.DerpiBooruAsync, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task GelBooru(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.GelbooruAsync, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task E621(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.E621Async, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Konachan(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.KonachanAsync, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Yandere(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.YandereAsync, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Rule34(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags);

            var data = await Rpc(ctx, _service.Rule34Async, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Hentai(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags, forceExplicit: true);

            var data = await Rpc(ctx, _service.HentaiAsync, payload);

            await NsfwReply(ctx, data);
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task HentaiBomb(ICommandContext ctx, params string[] tags)
        {
            var payload = GetTagRequest(ctx, tags, forceExplicit: true);

            var images = await Task.WhenAll(ErrorlessRpc(ctx, _service.GelbooruAsync, payload),
                                            ErrorlessRpc(ctx, _service.DanbooruAsync, payload),
                                            ErrorlessRpc(ctx, _service.KonachanAsync, payload),
                                            ErrorlessRpc(ctx, _service.YandereAsync, payload)).ConfigureAwait(false);

            var linksEnum = images?.Where(data => data != null && string.IsNullOrWhiteSpace(data.Error)).ToArray();
            if (images == null || !linksEnum.Any())
            {
                await ReplyErrorLocalizedAsync("not_found").ConfigureAwait(false);
                return;
            }

            await ctx.Channel.SendMessageAsync(string.Join("\n\n", linksEnum.Select(x => x.Url))).ConfigureAwait(false);
        }

        [NadekoCommand("nsfwtagbl")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task NsfwTagBlacklist(ICommandContext ctx, string tag = null)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                var reply = await Rpc(ctx, _service.GetBlacklsitedTagsAsync, new GetBlacklistedTagsMessage
                {
                    GuildId = ctx.Guild?.Id ?? 0,
                });

                await ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
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
                    GuildId = ctx.Guild?.Id ?? 0,
                });

                if (reply.IsAdded)
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_add", tag).ConfigureAwait(false);
                else
                    await ReplyConfirmLocalizedAsync("blacklisted_tag_remove", tag).ConfigureAwait(false);
            }

        }

        [NadekoCommand, Usage, Description, Aliases]
        [OwnerOnly]
        public async Task NsfwCc(ICommandContext ctx)
        {
            await Rpc(ctx, _service.ClearCacheAsync, new ClearCacheRequest { });

            await ctx.OkAsync();
        }
    }
}