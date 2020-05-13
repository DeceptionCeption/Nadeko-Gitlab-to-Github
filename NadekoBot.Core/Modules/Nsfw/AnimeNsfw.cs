using Discord.Commands;
using Nadeko.Microservices;
using NadekoBot.Common.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Nsfw
{
    public partial class Nsfw
    {
        [Group]
        [RequireNsfw]
        public class AnimeNsfw : NadekoSubmodule
        {
            private readonly SearchImages.SearchImagesClient _service;

            public AnimeNsfw(SearchImages.SearchImagesClient service)
            {
                _isNew = true;
                _service = service;
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

            [NadekoCommand]
            public async Task DanBooru(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.DanbooruAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task DerpiBooru(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.DerpiBooruAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task GelBooru(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.GelbooruAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task E621(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.E621Async, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task Konachan(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.KonachanAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task Yandere(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.YandereAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task Rule34(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags);

                var data = await Rpc(ctx, _service.Rule34Async, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task Hentai(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags, forceExplicit: true);

                var data = await Rpc(ctx, _service.HentaiAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task HentaiBomb(params string[] tags)
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

            [NadekoCommand]
            public async Task Safebooru(params string[] tags)
            {
                var payload = GetTagRequest(ctx, tags, forceExplicit: false);

                var data = await Rpc(ctx, _service.SafeBooruAsync, payload);

                await Nsfw.NsfwReply(ctx, data);
            }
        }
    }
}
