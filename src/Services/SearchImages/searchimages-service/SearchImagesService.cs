using Grpc.Core;
using Ayu.Common;
using Nadeko.Db;
using Nadeko.Microservices;
using Newtonsoft.Json.Linq;
using SearchImagesService.Common;
using SearchImagesService.Common.Db;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SearchImagesService
{
    public class SearchImagesService : SearchImages.SearchImagesBase
    {
        private readonly Random _rng;
        private readonly HttpClient _http;
        private readonly SearchImageCacher _cache;
        private readonly ServiceDb<SearchImageContext> _db;
        private readonly ConcurrentDictionary<ulong, HashSet<string>> _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>();

        public SearchImagesService()
        {
            _rng = new Random();
            _http = new HttpClient();
            _http.AddFakeHeaders();
            _cache = new SearchImageCacher();
            _db = new ServiceDb<SearchImageContext>(SearchImageContext.BaseOptions.Build());

            using (var uow = _db.GetDbContext())
            {
                _blacklistedTags = new ConcurrentDictionary<ulong, HashSet<string>>(uow.BlacklistedTags
                    .ToDictionary(x => x.GuildId, x => new HashSet<string>(x.Tags)));
            }
        }

        private Task<UrlReply> GetNsfwImageAsync(TagRequest request, ServerCallContext context, DapiSearchType dapi)
        {
            string[] tags = new string[request.Tags.Count];
            request.Tags.CopyTo(tags, 0);
            return GetNsfwImageAsync(request.GuildId, tags, request.ForceExplicit, dapi);
        }

        private bool IsValidTag(string tag) => tag.All(x => x != '+' && x != '?' && x != '/'); // tags mustn't contain + or ? or /

        private async Task<UrlReply> GetNsfwImageAsync(ulong guildId, string[] tags, bool forceExplicit, DapiSearchType dapi)
        {
            if (!tags.All(x => IsValidTag(x)))
            {
                return new UrlReply
                {
                    Error = "One or more tags are invalid.",
                    Url = ""
                };
            }

            Log.Information("Getting {V} image for Guild: {GuildId}...", dapi.ToString(), guildId);
            try
            {
                _blacklistedTags.TryGetValue(guildId, out var blTags);

                var result = await _cache.GetImage(tags, forceExplicit, dapi, blTags);

                if (result is null)
                {
                    return new UrlReply
                    {
                        Error = "Image not found.",
                        Url = ""
                    };
                }

                var reply = new UrlReply
                {
                    Error = "",
                    Url = result.FileUrl,
                    Rating = result.Rating,
                    Provider = result.SearchType.ToString()
                };

                reply.Tags.AddRange(result.Tags);

                return reply;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed getting {Dapi} image: {Message}", dapi, ex.Message);
                return new UrlReply
                {
                    Error = ex.Message,
                    Url = ""
                };
            }
        }

        public override Task<UrlReply> Gelbooru(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Gelbooru);

        public override Task<UrlReply> Danbooru(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Danbooru);

        public override Task<UrlReply> Konachan(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Konachan);

        public override Task<UrlReply> Yandere(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Yandere);

        public override Task<UrlReply> Rule34(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Rule34);

        public override Task<UrlReply> E621(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.E621);

        public override Task<UrlReply> DerpiBooru(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Derpibooru);

        public override Task<UrlReply> SafeBooru(TagRequest request, ServerCallContext context)
            => GetNsfwImageAsync(request, context, DapiSearchType.Safebooru);

        public override Task<ClearCacheReply> ClearCache(ClearCacheRequest request, ServerCallContext context)
        {
            _cache.Clear();
            return Task.FromResult(new ClearCacheReply());
        }

        public override async Task<UrlReply> Hentai(TagRequest request, ServerCallContext context)
        {
            // get all of the DAPI search types, except first 4
            // which are safebooru (not nsfw), and 2 furry ones 🤢
            // and rule 34 which is just bad
            var listOfProviders = Enum.GetValues(typeof(DapiSearchType))
                .Cast<DapiSearchType>()
                .Skip(4)
                .ToList();

            // now try to get an image, if it fails return an error,
            // keep trying for each provider until one of them is successful, or until 
            // we run out of providers. If we run out, then return an error
            UrlReply img;
            do
            {
                // random index of the providers
                var num = _rng.Next(0, listOfProviders.Count);
                // get the type
                var type = listOfProviders[num];
                // remove it 
                listOfProviders.RemoveAt(num);
                // get the image
                img = await GetNsfwImageAsync(request, context, type).ConfigureAwait(false);
                // if i can't find the image, ran out of providers, or tag is blacklisted
                // return the error

                if (img.Error == "")
                    break;

            } while (listOfProviders.Any());

            return img;
        }

        public override async Task<UrlReply> Boobs(BoobsRequest request, ServerCallContext context)
        {
            try
            {
                JToken obj;
                obj = JArray.Parse(await _http.GetStringAsync($"http://api.oboobs.ru/boobs/{_rng.Next(0, 12000)}").ConfigureAwait(false))[0];
                return new UrlReply
                {
                    Error = "",
                    Url = $"http://media.oboobs.ru/{obj["preview"]}",
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retreiving boob image: {Message}", ex.Message);
                return new UrlReply
                {
                    Error = ex.Message,
                    Url = "",
                };
            }
        }

        private readonly object taglock = new object();
        public override Task<BlacklistTagReply> ToggleBlacklistTag(BlacklistTagMessage request, ServerCallContext context)
        {
            lock (taglock)
            {
                var tag = request.Tag.Trim().ToLowerInvariant();
                var blacklistedTags = _blacklistedTags.GetOrAdd(request.GuildId, new HashSet<string>());
                var isAdded = blacklistedTags.Add(tag);

                if (!isAdded)
                {
                    blacklistedTags.Remove(tag);
                }

                var tagArr = blacklistedTags.ToArray();

                using (var uow = _db.GetDbContext())
                {
                    var bt = uow.BlacklistedTags.FirstOrDefault(x => x.GuildId == request.GuildId);
                    if (bt is null)
                    {
                        uow.BlacklistedTags.Add(new Common.Db.Models.BlacklistedTags
                        {
                            GuildId = request.GuildId,
                            Tags = tagArr
                        });
                    }
                    else
                    {
                        bt.Tags = tagArr;
                    }

                    uow.SaveChanges();
                }

                return Task.FromResult(new BlacklistTagReply
                {
                    IsAdded = isAdded
                });
            }
        }

        public override Task<GetBlacklistedTagsReply> GetBlacklsitedTags(GetBlacklistedTagsMessage request, ServerCallContext context)
        {
            if (_blacklistedTags.TryGetValue(request.GuildId, out var tags))
            {
                var toReturn = new GetBlacklistedTagsReply();

                toReturn.BlacklistedTags.AddRange(tags);

                return Task.FromResult(toReturn);
            }

            return Task.FromResult(new GetBlacklistedTagsReply
            {
            });
        }

        public override async Task<UrlReply> Butts(ButtsRequest request, ServerCallContext context)
        {
            try
            {
                JToken obj;
                obj = JArray.Parse(await _http.GetStringAsync($"http://api.obutts.ru/butts/{_rng.Next(0, 6100)}").ConfigureAwait(false))[0];
                return new UrlReply
                {
                    Error = "",
                    Url = $"http://media.obutts.ru/{obj["preview"]}",
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retreiving butt image: {Message}", ex.Message);
                return new UrlReply
                {
                    Error = ex.Message,
                    Url = "",
                };
            }
        }
    }
}

