using Discord.Commands;
using Nadeko.Microservices;
using NadekoBot.Common.Attributes;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Nsfw
{
    public partial class Nsfw
    {
        [Group]
        [RequireNsfw]
        public class RealNsfw : NadekoSubmodule
        {
            private readonly SearchImages.SearchImagesClient _service;

            public RealNsfw(SearchImages.SearchImagesClient service)
            {
                _isNew = true;
                _service = service;
            }

            [NadekoCommand]
            public async Task Boobs()
            {
                var data = await Rpc(ctx, _service.BoobsAsync, new BoobsRequest { });

                await Nsfw.NsfwReply(ctx, data);
            }

            [NadekoCommand]
            public async Task Butts()
            {
                var data = await Rpc(ctx, _service.ButtsAsync, new ButtsRequest { });

                await Nsfw.NsfwReply(ctx, data);
            }
        }
    }
}
