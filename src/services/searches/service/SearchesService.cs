using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ayu.Common;
using Nadeko.Microservices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SearchesService.Models;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Channels;
using Channel = System.Threading.Channels.Channel;
using System.Collections.Concurrent;
using System.Threading;
using Nadeko.Common;
using AngleSharp.Common;

// todo 3.1 str commands_instr
// todo 3.1 str animal_race_join_instr
namespace SearchesService
{
    public class SearchesService : Searches.SearchesBase
    {
        private const string omdb_api_url = "https://omdbapi.nadeko.bot/?t={0}&y=&plot=full&r=json";

        private readonly Random _rng;
        private readonly HttpClient _http;
        private readonly ILogger _log;

        private readonly ServiceConfig<SearchesCreds> realSearchCreds;
        private SearchesCreds SearchCreds => realSearchCreds.Data;
        private readonly IConfiguration _browsingConfig;
        private readonly ConnectionMultiplexer _redis;
        private readonly IResultCache _cache;

        private IReadOnlyDictionary<string, SearchPokemon> Pokemons { get; set; }
        private IReadOnlyDictionary<string, SearchPokemonAbility> PokemonAbilities { get; set; }
        private IReadOnlyDictionary<int, string> PokemonMap { get; set; }

        private const string pokemonAbilitiesFile = "config/pokemon/pokemon_abilities.json";
        private const string pokemonListFile = "config/pokemon/pokemon_list.json";
        private const string pokemonMapPath = "config/pokemon/name-id_map.json";

        public SearchesService(CredsService creds)
        {
            _log = Log.Logger;
            // i can't deal with this rn
            realSearchCreds = new ServiceConfig<SearchesCreds>("config/searches.yml");
            _http = new HttpClient();
            _rng = new Random();
            _http.DefaultRequestHeaders.Clear();
            _browsingConfig = AngleSharp.Configuration.Default.WithDefaultLoader();

            _redis = creds.GetRedisConnectionMultiplexer();
            _cache = new RedisResultCache(_redis);
            LoadPokemans();
        }

        private void LoadPokemans()
        {
            if (!File.Exists(pokemonListFile))
            {
                Log.Warning(pokemonListFile + " is missing. Pokemon abilities not loaded.");
            }
            else
            {
                Pokemons = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemon>>(File.ReadAllText(pokemonListFile))
                    .ToDictionary(x => x.Key.ToUpperInvariant(), x => x.Value);
            }

            if (!File.Exists(pokemonAbilitiesFile))
            {
                Log.Warning(pokemonAbilitiesFile + " is missing. Pokemon abilities not loaded.");
            }
            else
            {
                PokemonAbilities = JsonConvert.DeserializeObject<Dictionary<string, SearchPokemonAbility>>(File.ReadAllText(pokemonAbilitiesFile))
                    .ToDictionary(x => x.Key.ToUpperInvariant(), x => x.Value);
            }

            if (!File.Exists(pokemonMapPath))
            {

                Log.Warning(pokemonMapPath + " is missing. Pokemon abilities not loaded.");
            }
            else
            {
                PokemonMap = JsonConvert.DeserializeObject<PokemonNameId[]>(File.ReadAllText(pokemonMapPath))
                           .ToDictionary(x => x.Id, x => x.Name);
            }
        }

        public override Task<HostedPicReply> GetRandomHostedPic(GetPicRequest request, ServerCallContext context)
        {
            var subpath = request.Type.ToString().ToLowerInvariant();
            // todo 3.3 add more images
            var max = request.Type switch
            {
                GetPicRequest.Types.PicType.Food => 773,
                GetPicRequest.Types.PicType.Dogs => 750,
                GetPicRequest.Types.PicType.Cats => 773,
                GetPicRequest.Types.PicType.Birds => 578,
                _ => 100,
            };
            var url = $"https://nadeko-pictures.nyc3.digitaloceanspaces.com/{subpath}/" +
                _rng.Next(1, max).ToString("000") + ".png";

            _log.Information("Sending {Type} pic: {Url}", request.Type, url);

            return Task.FromResult(new HostedPicReply()
            {
                Url = url
            });
        }

        public override async Task<CatFactReply> GetCatFact(CatFactRequest request, ServerCallContext context)
        {
            string response;
            try
            {
                response = await _http.GetStringAsync("https://catfact.ninja/fact").ConfigureAwait(false);

                var fact = JObject.Parse(response)["fact"].ToString();

                if (string.IsNullOrWhiteSpace(fact))
                {
                    _log.Error("Catfact is empty. Response: {Response}", response);
                    fact = "-";
                }

                _log.Information("Sending catfact: {Fact}", fact);

                return new CatFactReply
                {
                    Data = new CatFactReply.Types.Info
                    {
                        Fact = fact
                    }
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting catfact: {Message}", ex.Message);
                return new CatFactReply
                {
                    Error = Errors.InvalidInput
                };
            }
        }

        public override async Task<MovieData> GetMovie(MovieRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim().Replace(' ', '+');

            if (string.IsNullOrWhiteSpace(query))
            {
                return new MovieData
                {
                    Error = Errors.InvalidInput
                };
            }

            OmdbMovie movie;
            try
            {
                var res = await _cache.GetOrAddStringAsync($"omdb_{query}",
                    _ => _http.GetStringAsync(string.Format(omdb_api_url, query)),
                    TimeSpan.FromHours(3)).ConfigureAwait(false);

                movie = JsonConvert.DeserializeObject<OmdbMovie>(res);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting movie '{Query}': {Message}", query, ex.Message);
                return new MovieData
                {
                    Error = Errors.Unknown
                };
            }

            if (movie?.Title is null)
            {
                Log.Warning("Movie not found: {Query}", query);
                return new MovieData
                {
                    Error = Errors.NotFound
                };
            }

            _log.Information("Sending movie data: {Query}", query);

            // movie.Poster = await _google.ShortenUrl(movie.Poster).ConfigureAwait(false);
            return new MovieData
            {
                Data = new MovieData.Types.Info
                {
                    Genre = movie.Genre,
                    ImdbId = movie.ImdbId,
                    Rating = movie.ImdbRating,
                    Plot = movie.Plot,
                    Poster = movie.Poster,
                    Title = movie.Title,
                    Year = movie.Year,
                }
            };
        }

        public override async Task<BibleVerse> GetBibleVerse(BibleRequest request, ServerCallContext context)
        {
            var bookStr = request.Book.Trim();
            var verseStr = request.ChapterAndVerse.Trim();

            if (string.IsNullOrWhiteSpace(bookStr) | string.IsNullOrEmpty(verseStr))
            {
                return new BibleVerse
                {
                    Error = Errors.InvalidInput
                };
            }

            try
            {
                var res = await _cache.GetOrAddStringAsync($"bible_{bookStr} {verseStr}",
                    _ => _http.GetStringAsync($"https://bible-api.com/{request.Book} {verseStr}"),
                    TimeSpan.FromDays(1)).ConfigureAwait(false);

                var obj = JsonConvert.DeserializeObject<BibleVerses>(res);

                var verse = obj.Verses[0];

                _log.Information("Sending bible verse for: {Book} {ChapterAndVerse}", bookStr, verseStr);

                return new BibleVerse
                {
                    Data = new BibleVerse.Types.Info
                    {
                        BookName = verse.BookName,
                        Chapter = verse.Chapter,
                        Verse = verse.Verse,
                        Text = verse.Text
                    }
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error retrieving bible verse {Book} {ChapterAndVerse}", bookStr, verseStr);
                return new BibleVerse
                {
                    Error = Errors.InvalidInput
                };
            }
        }

        // nuked because gampedia api is literally terrible
        //public override async Task<GamepediaReply> GetGamepadiaPage(GamepediaRequest request, ServerCallContext context)
        //{
        //    var gt = new GetTextProvider(context, _localization);

        //    var query = request.Query.Trim();
        //    var target = request.Target.Trim();

        //    if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
        //    {
        //        throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid target or query."));
        //    }

        //    string res = null;
        //    try
        //    {
        //        // https://mtg.gamepedia.com/api.php?action=help&modules=query%2Bsearch
        //        res = await _cache.GetOrAddStringAsync($"gamepedia_{target}_{query}",
        //            _ => _http.GetStringAsync($"https://{Uri.EscapeDataString(target)}.gamepedia.com/api.php?" +
        //            $"action=query&" +
        //            $"list=search&" +
        //            $"srsearch={Uri.EscapeDataString(query)}&" +
        //            $"format=json&" +
        //            $"srlimit=1&" +
        //            $"srprop=snippet|title"),
        //            TimeSpan.FromHours(3)).ConfigureAwait(false);

        //        var items = JsonConvert.DeserializeObject<GamepediaResponse>(res);

        //        if (items.Query.Search.Length == 0)
        //        {
        //            return new GamepediaReply
        //            {
        //                Error = "not_found"
        //            };
        //        }

        //        var found = items.Query.Search[0];

        //        _log.Information("Sending gamepedia data for: {Query}", query);

        //        return new GamepediaReply
        //        {
        //            Data = new GamepediaReply.Types.GamepediaData()
        //            {
        //                Snippet = found.Snippet.StripHTML(),
        //                Title = found.Title,
        //                Url = $"https://{Uri.EscapeDataString(target)}.gamepedia.com/{Uri.EscapeDataString(query)}",
        //            }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _log.Error(ex, "Error retrieving gamepedia info for: '{Target}' '{Query}'", request.Target, query);
        //        throw new RpcException(new Status(StatusCode.NotFound, gt.GetText("not_found")));
        //    }
        //}

        public override async Task<WikipediaReply> GetWikipediaPage(WikipediaRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim();
            if (string.IsNullOrEmpty(query))
            {
                return new WikipediaReply
                {
                    Error = Errors.InvalidInput
                };
            }

            try
            {
                var result = await _cache.GetOrAddStringAsync($"wikipedia_{query}",
                    _ => _http.GetStringAsync("https://en.wikipedia.org/w/api.php?action=query" +
                        "&format=json" +
                        "&prop=info" +
                        "&redirects=1" +
                        "&formatversion=2" +
                        "&inprop=url" +
                        "&titles=" + Uri.EscapeDataString(query)),
                    TimeSpan.FromHours(1)).ConfigureAwait(false);

                var data = JsonConvert.DeserializeObject<WikipediaApiModel>(result);

                if (data.Query.Pages is null || !data.Query.Pages.Any() || data.Query.Pages.First().Missing)
                {
                    return new WikipediaReply
                    {
                        Error = Errors.NotFound
                    };
                }

                _log.Information("Sending wikipedia url for: {Query}", query);

                return new WikipediaReply
                {
                    Data = new WikipediaReply.Types.Info
                    {
                        Url = data.Query.Pages[0].FullUrl,
                    }
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error retrieving wikipedia data for: '{Query}'", query);
                return new WikipediaReply
                {
                    Error = Errors.Unknown
                };
            }
        }

        //public override async Task<HashTagData> GetHashtagDefinition(HashTagRequest request, ServerCallContext context)
        //{
        //    var gt = new GetTextProvider(context, _localization);
        //    var query = request.Query.Trim();
        //    if (string.IsNullOrEmpty(query))
        //    {
        //        throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid query."));
        //    }

        //    using (var msg = new HttpRequestMessage(HttpMethod.Get, $"https://tagdef.p.rapidapi.com/one.{Uri.EscapeDataString(query)}.json"))
        //    {
        //        if (string.IsNullOrWhiteSpace(_creds.RapidApiKey))
        //        {
        //            throw new RpcException(new Status(StatusCode.Unauthenticated, gt.GetText("api_key_missing")));
        //        }
        //        try
        //        {
        //            msg.Headers.TryAddWithoutValidation("X-Mashape-Key", _creds.RapidApiKey);
        //            msg.Headers.TryAddWithoutValidation("x-rapidapi-host", "tagdef.p.rapidapi.com");

        //            using (var response = await _http.SendAsync(msg).ConfigureAwait(false))
        //            {
        //                response.EnsureSuccessStatusCode();
        //                var text = await _cache.GetOrAddStringAsync("hashtag_{query}",
        //                    _ => response.Content.ReadAsStringAsync(),
        //                    TimeSpan.FromHours(3)).ConfigureAwait(false);
        //                var items = JsonConvert.DeserializeObject<HashTagResponse>(text);
        //                var def = items.Definitions.Definition;

        //                _log.Information("Sending hashtag data for: {Query}", query);

        //                return new HashTagData
        //                {
        //                    Name = query,
        //                    Text = def.Text,
        //                    Url = def.Uri,
        //                    Icon = "http://res.cloudinary.com/urbandictionary/image/upload/a_exif,c_fit,h_200,w_200/v1394975045/b8oszuu3tbq7ebyo7vo1.jpg",
        //                };
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _log.Error(ex, "Error retrieving hashtag data for: {Query}", query);
        //            throw new RpcException(new Status(StatusCode.NotFound, gt.GetText("not_found")));
        //        }
        //    }
        //}

        public override async Task<DefineResponse> GetDefinition(DefineRequest request, ServerCallContext context)
        {
            string word = request.Word.Trim();

            if (string.IsNullOrEmpty(word))
            {
                return new DefineResponse
                {
                    Error = Errors.InvalidInput
                };
            }

            string res;
            try
            {
                res = await _cache.GetOrAddStringAsync($"define_{word}",
                    _ => _http.GetStringAsync("https://api.pearson.com/v2/dictionaries/entries?headword=" + WebUtility.UrlEncode(word)),
                    TimeSpan.FromHours(3)).ConfigureAwait(false);

                var data = JsonConvert.DeserializeObject<DefineModel>(res);

                var datas = data.Results
                    .Where(x => !(x.Senses is null) && x.Senses.Count > 0 && !(x.Senses[0].Definition is null))
                    .Select(x => (Sense: x.Senses[0], x.PartOfSpeech));

                if (!datas.Any())
                {
                    Log.Warning("Definition not found: {Word}", word);
                    return new DefineResponse
                    {
                        Error = Errors.NotFound
                    };
                }


                var col = datas.Select(data => new DefineData
                {
                    Definition = data.Sense.Definition is string
                        ? data.Sense.Definition.ToString()
                        : ((JArray)JToken.Parse(data.Sense.Definition.ToString())).First.ToString(),
                    Example = data.Sense.Examples is null || data.Sense.Examples.Count == 0
                        ? string.Empty
                        : data.Sense.Examples[0].Text,
                    Word = word,
                    WordType = data.PartOfSpeech,
                }).ToList();

                _log.Information("Sending {Count} definition for: {Word}", col.Count, word);

                var toReturn = new DefineResponse()
                {
                    Data = new DefineResponse.Types.Info()
                };
                toReturn.Data.Datas.AddRange(col);
                return toReturn;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error retrieving definition data for: {Word}", word);
                return new DefineResponse
                {
                    Error = Errors.Unknown
                };
            }
        }

        public override async Task<UrbanDictReply> GetUrbanDictDefinition(UrbanDictRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim();
            if (string.IsNullOrEmpty(query))
            {
                return new UrbanDictReply
                {
                    Error = Errors.InvalidInput
                };
            }

            string res;
            try
            {
                res = await _cache.GetOrAddStringAsync($"urbandict_{query}",
                    _ => _http.GetStringAsync($"https://api.urbandictionary.com/v0/define?term={Uri.EscapeDataString(query)}"),
                    TimeSpan.FromHours(3)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error retrieving hashtag data for: '{Query}'", query);
                return new UrbanDictReply
                {
                    Error = Errors.NotFound
                };
            }
            var items = JsonConvert.DeserializeObject<UrbanResponse>(res).List;

            if (items.Length == 0)
            {
                Log.Warning("Can't find urbandict data for: '{Query}'", query);
                return new UrbanDictReply
                {
                    Error = Errors.NotFound
                };
            }

            var defs = items.Select(x => new UrbanDictReply.Types.UrbanDictData
            {
                Definition = x.Definition,
                Url = x.Permalink,
                Word = x.Word,
            });

            var reply = new UrbanDictReply()
            {
                Data = new UrbanDictReply.Types.DefinitionsData()
            };

            reply.Data.Definitions.AddRange(defs);

            _log.Information("Sending urbandict data for: {Query}", query);

            return reply;
        }

        public override async Task<HearthstoneCardData> GetHearthstoneCard(HearthstoneCardRequest request, ServerCallContext context)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return new HearthstoneCardData
                {
                    Error = Errors.InvalidInput
                };
            }

            var url = $"https://omgvamp-hearthstone-v1.p.rapidapi.com/cards/search/{ Uri.EscapeDataString(name) }";

            var rapidApiKey = SearchCreds.RapidApiKey;
            if (string.IsNullOrWhiteSpace(rapidApiKey))
            {
                return new HearthstoneCardData
                {
                    Error = Errors.ApiKeyMissing
                };
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            string response = null;
            req.Headers.TryAddWithoutValidation("x-rapidapi-key", rapidApiKey);
            req.Headers.TryAddWithoutValidation("x-rapidapi-host", "omgvamp-hearthstone-v1.p.rapidapi.com");
            try
            {
                response = await _cache.GetOrAddStringAsync($"hearthstone_{name}", async _ =>
                {
                    using var resMsg = await _http.SendAsync(req).ConfigureAwait(false);
                    return await resMsg.Content.ReadAsStringAsync();
                }, TimeSpan.FromHours(3));

                var objs = JsonConvert.DeserializeObject<HearthstoneApiResponse[]>(response);

                var data = objs.FirstOrDefault(x => x.Collectible)
                    ?? objs.FirstOrDefault(x => !string.IsNullOrEmpty(x.PlayerClass))
                    ?? objs.FirstOrDefault();

                if (data is null)
                {
                    return new HearthstoneCardData
                    {
                        Error = Errors.NotFound
                    };
                }

                if (!string.IsNullOrWhiteSpace(data.Text))
                {
                    var converter = new Html2Markdown.Converter();
                    data.Text = converter.Convert(data.Text);
                }

                _log.Information("Sending hearthstone data for: {Query}\nImage: {Img}", name, data.Img);

                return new HearthstoneCardData
                {
                    Data = new HearthstoneCardData.Types.HsData
                    {
                        Flavor = data.Flavor,
                        Url = data.Img
                    }
                };
            }
            catch (JsonSerializationException)
            {
                return new HearthstoneCardData
                {
                    Error = Errors.NotFound
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting hs card data: {Message}", ex.Message);
                return new HearthstoneCardData
                {
                    Error = Errors.ApiKeyMissing
                };
            }

        }

        public override async Task<MtgCardData> GetMtgCard(MtgCardRequest request, ServerCallContext context)
        {
            var queryUrl = $"https://api.scryfall.com/cards/search?" +
                $"q={Uri.EscapeDataString(request.Query)}";

            var resString = await _http.GetStringAsync(queryUrl).ConfigureAwait(false);
            var resData = JsonConvert.DeserializeObject<MtgApiData>(resString);

            if (resData.Data is null || resData.Data.Count == 0)
            {
                return new MtgCardData
                {
                    Error = Errors.NotFound
                };
            }

            var randData = resData.Data[_rng.Next(0, resData.Data.Count)];
            var info = new MtgCardData.Types.Info
            {
                Cost = randData.ManaCost,
                Description = randData.OracleText,
                ImageUrl = randData.Images.Normal,
                Name = randData.Name,
                Types_ = randData.TypeLine,
                Flavor = randData.Flavor ?? string.Empty,
                Url = randData.ScryfallUrl
            };

            info.StoreUrls.Add(randData.PurchaseUrls);
            return new MtgCardData
            {
                Data = info
            };
        }

        public override async Task<GoogleSearchResult> SearchGoogle(GoogleSearchRequest request, ServerCallContext context)
        {
            var query = WebUtility.UrlEncode(request.Query).Replace(' ', '+');
            if (string.IsNullOrEmpty(query))
            {
                return new GoogleSearchResult
                {
                    Error = Errors.InvalidInput
                };
            }

            var fullQueryLink = $"https://www.google.ca/search?q={ query }&safe=on&lr=lang_eng&hl=en&ie=utf-8&oe=utf-8";

            using var msg = new HttpRequestMessage(HttpMethod.Get, fullQueryLink);
            msg.Headers.AddFakeHeaders();
            var parser = new HtmlParser();
            var test = "";
            using var response = await _http.SendAsync(msg).ConfigureAwait(false);
            using var document = await parser.ParseDocumentAsync(test = await response.Content.ReadAsStringAsync().ConfigureAwait(false)).ConfigureAwait(false);
            var elems = document.QuerySelectorAll("div.g");

            var resultsElem = document.QuerySelectorAll("#resultStats").FirstOrDefault();
            var totalResults = resultsElem?.TextContent;

            if (!elems.Any())
            {
                Log.Warning("No google search results found for query: {Query}", query);
                return new GoogleSearchResult
                {
                    Error = Errors.NotFound
                };
            }

            var results = elems.Select(elem =>
            {
                var aTag = elem.QuerySelector("a") as IHtmlAnchorElement; // <h3> -> <a>
                var href = aTag?.Href;

                var name = aTag?.QuerySelector("h3")?.TextContent;
                if (href is null || name is null)
                    return null;

                var txt = elem.QuerySelectorAll(".st").FirstOrDefault()?.TextContent;

                if (txt is null)
                    return null;

                return new GoogleSearchResult.Types.FullData.Types.Data
                {
                    Name = name,
                    Url = href,
                    Text = txt,
                };
            }).Where(x => x != null).Take(5);

            if (!results.Any())
            {
                Log.Warning("No google search results found for: {Query}", query);
                return new GoogleSearchResult
                {
                    Error = Errors.NotFound
                };
            }

            _log.Information("Sending google search results for: {Query}", query);

            var result = new GoogleSearchResult.Types.FullData
            {
                FullQueryLink = fullQueryLink,
                Query = query,
                TotalResults = totalResults,
            };

            result.Results.AddRange(results);

            return new GoogleSearchResult
            {
                Data = result
            };
        }

        public class ShortenData
        {
            [JsonProperty("result_url")]
            public string ResultUrl { get; set; }
        }

        private readonly ConcurrentDictionary<string, string> cachedShortenedLinks = new ConcurrentDictionary<string, string>();
        public override async Task<ShortenUrlReply> ShortenUrl(ShortenUrlRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim();
            if (cachedShortenedLinks.TryGetValue(query, out var shortLink))
            {
                return new ShortenUrlReply
                {
                    Url = shortLink
                };
            }
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://goolnk.com/api/v1/shorten");
                //req.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                var formData = new MultipartFormDataContent
                {
                    { new StringContent(query), "url" }
                };
                req.Content = formData;

                using var res = await _http.SendAsync(req).ConfigureAwait(false);
                var content = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<ShortenData>(content);

                if (!string.IsNullOrWhiteSpace(data?.ResultUrl))
                    cachedShortenedLinks.TryAdd(query, data.ResultUrl);

                return new ShortenUrlReply
                {
                    Url = data.ResultUrl
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error shortening a link: {Message}", ex.Message);
                return new ShortenUrlReply
                {
                    Url = ""
                };
            }
        }

        public override async Task<ImageSearchReply> ImageSearch(ImageSearchRequest request, ServerCallContext context)
        {
            var query = WebUtility.UrlEncode(request.Query).Replace(' ', '+');

            if (string.IsNullOrEmpty(query))
            {
                return new ImageSearchReply
                {
                    Error = Errors.InvalidInput
                };
            }

            var fullQueryLink = $"https://imgur.com/search?q={ query }";
            using var document = await BrowsingContext.New(_browsingConfig).OpenAsync(fullQueryLink).ConfigureAwait(false);
            var elems = document.QuerySelectorAll("a.image-list-link").ToList();

            var img = (elems.ElementAtOrDefault(_rng.Next(0, elems.Count))?.Children?.FirstOrDefault() as IHtmlImageElement);

            if (img?.Source is null)
            {
                return new ImageSearchReply
                {
                    Error = Errors.NotFound
                };
            }

            var source = img.Source.Replace("b.", ".", StringComparison.InvariantCulture);

            _log.Information("Sending image search results for: {Query}", query);

            return new ImageSearchReply
            {
                Data = new ImageSearchReply.Types.Info
                {
                    Url = source,
                    ProviderIconUrl = "https://s.imgur.com/images/favicon-32x32.png",
                    Query = fullQueryLink,
                }
            };
        }

        public override async Task<GetTimeReply> GetTime(GetTimeRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim();

            if (string.IsNullOrEmpty(query))
            {
                return new GetTimeReply
                {
                    Error = Errors.InvalidInput
                };
            }

            var creds = SearchCreds;
            if (string.IsNullOrWhiteSpace(creds.LocationIqApiKey)
                || string.IsNullOrWhiteSpace(creds.TimezoneDbApiKey))
            {
                return new GetTimeReply
                {
                    Error = Errors.ApiKeyMissing
                };
            }

            try
            {
                var res = await _cache.GetOrAddStringAsync($"geo_{query}", _ =>
                {
                    var url = "https://eu1.locationiq.com/v1/search.php?" +
                        (string.IsNullOrWhiteSpace(creds.LocationIqApiKey) ? "key=" : $"key={creds.LocationIqApiKey}&") +
                        $"q={Uri.EscapeDataString(query)}&" +
                        $"format=json";

                    var res = _http.GetStringAsync(url);
                    return res;
                }, TimeSpan.FromHours(1));

                var responses = JsonConvert.DeserializeObject<LocationIqResponse[]>(res);
                if (responses is null || responses.Length == 0)
                {
                    Log.Warning("Geocode lookup failed for: {Query}", query);
                    return new GetTimeReply
                    {
                        Error = Errors.NotFound
                    };
                }

                var geoData = responses[0];

                using var req = new HttpRequestMessage(HttpMethod.Get, "http://api.timezonedb.com/v2.1/get-time-zone?" +
                    $"key={creds.TimezoneDbApiKey}&format=json&" +
                    "by=position&" +
                    $"lat={geoData.Lat}&lng={geoData.Lon}");

                using var geoRes = await _http.SendAsync(req);
                var timeObj = JsonConvert.DeserializeObject<TimeZoneResult>(await geoRes.Content.ReadAsStringAsync());

                var time = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(timeObj.Timestamp);

                return new GetTimeReply
                {
                    Data = new GetTimeReply.Types.Info
                    {
                        Address = responses[0].DisplayName,
                        Time = Timestamp.FromDateTime(DateTime.SpecifyKind(time, DateTimeKind.Utc)),
                        TimeZoneName = timeObj.TimezoneName,
                    }
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Weather error: {Message}", ex.Message);
                return new GetTimeReply
                {
                    Error = Errors.NotFound
                };
            }
        }

        public override async Task<WeatherData> GetWeather(WeatherRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim();
            if (string.IsNullOrEmpty(query))
            {
                return new WeatherData
                {
                    Error = Errors.InvalidInput
                };
            }
            try
            {
                Task<string> GetWeatherFactory() => _http.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?" +
                            $"q={Uri.EscapeDataString(query)}&" +
                            $"appid=42cd627dd60debf25a5739e50a217d74&" +
                            $"units=metric");

                var stringData = await _cache.GetOrAddStringAsync($"weather_{request.Query}", _ => GetWeatherFactory(), TimeSpan.FromHours(1));

                var data = JsonConvert.DeserializeObject<WeatherApiData>(stringData);

                _log.Information("Sending weather data for: {Query}", query);

                return new WeatherData
                {
                    Data = new WeatherData.Types.Info
                    {
                        Location = data.Name + ", " + data.Sys.Country,
                        Coords = $"{data.Coord.Lat}, {data.Coord.Lon}",
                        Condition = string.Join(", ", data.Weather.Select(w => w.Main)),
                        Humidity = data.Main.Humidity,
                        WindSpeed = data.Wind.Speed,
                        Temperature = data.Main.Temp,
                        TempMax = data.Main.TempMax,
                        TempMin = data.Main.TempMin,
                        Url = $"https://openweathermap.org/city/{data.Id}",
                        Sunrise = data.Sys.Sunrise,
                        Sunset = data.Sys.Sunset,
                        Source = "Powered by openweathermap.org",
                        SourceIcon = $"http://openweathermap.org/img/w/{data.Weather[0].Icon}.png",
                    }
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Weather error: {Message}", ex.Message);
                return new WeatherData
                {
                    Error = Errors.NotFound
                };
            }
        }

        public override async Task<AnimeData> GetAnime(AnimeRequest request, ServerCallContext context)
        {
            var query = request.Name.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(query))
            {
                return new AnimeData
                {
                    Error = Errors.InvalidInput
                };
            }

            try
            {
                async Task<string> GetAnimeFactoryAsync()
                {
                    var link = "https://aniapi.nadeko.bot/anime/" + Uri.EscapeDataString(query.Replace("/", " ", StringComparison.InvariantCulture));

                    var req = new HttpRequestMessage(HttpMethod.Get, link);

                    using var source = new CancellationTokenSource();
                    source.CancelAfter(5000);
                    using var res = await _http.SendAsync(req, source.Token);
                    res.EnsureSuccessStatusCode();
                    var animeData = await res.Content.ReadAsStringAsync();
                    return animeData;
                }

                string data;
                try
                {
                    data = await _cache.GetOrAddStringAsync($"anime_{query}", _ => GetAnimeFactoryAsync(), TimeSpan.FromDays(1));
                }
                catch (OperationCanceledException)
                {
                    return new AnimeData
                    {
                        Error = Errors.NotFound,
                    };
                }
                var result = JsonConvert.DeserializeObject<AnimeResult>(data);

                if (string.IsNullOrWhiteSpace(result.TitleEnglish))
                {
                    return new AnimeData
                    {
                        Error = Errors.NotFound,
                    };
                }

                var toReturn = new AnimeData.Types.Info
                {
                    AiringStatus = result.AiringStatus,
                    AverageScore = result.AverageScore,
                    Id = result.Id,
                    ImageUrlLarge = result.ImageUrlLarge,
                    Link = result.Link,
                    Synopsis = result.Synopsis,
                    TitleEnglish = result.TitleEnglish,
                    TotalEpisodes = result.TotalEpisodes,
                };

                if (result.Genres.Any())
                    toReturn.Genres.AddRange(result.Genres);
                else
                    toReturn.Genres.Add("-");

                _log.Information("Sending anime: {Query}", query);

                return new AnimeData
                {
                    Data = toReturn,
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting anime data: {Message}", ex.Message);

                return new AnimeData
                {
                    Error = Errors.NotFound
                };
            }
        }

        public override async Task<MangaData> GetManga(MangaRequest request, ServerCallContext context)
        {
            var query = request.Query.Trim();

            if (string.IsNullOrEmpty(query))
            {
                return new MangaData
                {
                    Error = Errors.InvalidInput
                };
            }

            try
            {
                async Task<string> GetMangaFactoryAsync()
                {
                    var link = "https://aniapi.nadeko.bot/manga/" + Uri.EscapeDataString(query.Replace("/", " ", StringComparison.InvariantCulture));
                    var req = new HttpRequestMessage(HttpMethod.Get, link);

                    using var source = new CancellationTokenSource();
                    source.CancelAfter(5000);
                    using var res = await _http.SendAsync(req, source.Token);
                    res.EnsureSuccessStatusCode();
                    var mangaData = await res.Content.ReadAsStringAsync();
                    return mangaData;
                }

                string data;
                try
                {
                    data = await _cache.GetOrAddStringAsync($"manga_{query}", _ => GetMangaFactoryAsync(), TimeSpan.FromDays(1));
                }
                catch (OperationCanceledException)
                {
                    return new MangaData
                    {
                        Error = Errors.NotFound,
                    };
                }
                var result = JsonConvert.DeserializeObject<MangaResult>(data);

                if (string.IsNullOrWhiteSpace(result.TitleEnglish))
                {
                    return new MangaData
                    {
                        Error = Errors.NotFound,
                    };
                }


                var genres = new Google.Protobuf.Collections.RepeatedField<string>();

                genres.AddRange(result.Genres);

                var toReturn = new MangaData.Types.Info
                {
                    PublishingStatus = result.PublishingStatus,
                    AverageScore = result.AverageScore,
                    Id = result.Id,
                    ImageUrlLarge = result.ImageUrlLge,
                    Link = result.Link,
                    Synopsis = result.Synopsis,
                    TitleEnglish = result.TitleEnglish,
                    TotalChapters = result.TotalChapters,
                    TotalvVolumes = result.TotalVolumes,
                };

                if (result.Genres.Any())
                    toReturn.Genres.AddRange(result.Genres);
                else
                    toReturn.Genres.Add("-");

                _log.Information("Sending anime: {Query}", query);

                return new MangaData
                {
                    Data = toReturn
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting anime data: {Message}", ex.Message);
                return new MangaData
                {
                    Error = Errors.NotFound
                };
            }
        }

        public override async Task<XkcdReply> GetXkcdComic(XkcdRequest request, ServerCallContext context)
        {
            var number = request.Number;
            if (number < -1)
            {
                return new XkcdReply
                {
                    Error = Errors.InvalidInput
                };
            }

            try
            {
                string link;
                if (number >= 0)
                {
                    link = $"https://xkcd.com/{number}/info.0.json";
                }
                else
                {
                    link = $"https://xkcd.com/info.0.json";
                }

                var res = await _http.GetStringAsync(link).ConfigureAwait(false);

                _log.Information("Sending xkcd #{Number}", number);

                var info = JsonConvert.DeserializeObject<XkcdReply.Types.Info>(res);

                return new XkcdReply
                {
                    Data = info
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting anime data: {Message}", ex.Message);
                return new XkcdReply
                {
                    Error = Errors.NotFound
                };
            }
        }

        public override async Task<CryptoData> GetCryptoData(CryptoRequest request, ServerCallContext context)
        {
            var name = request.Name.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(name))
            {
                return new CryptoData
                {
                    Error = Errors.InvalidInput
                };
            }

            var cryptoData = await InternalGetCryptoDataAsync().ConfigureAwait(false);

            var crypto = cryptoData?.FirstOrDefault(x => x.Id.Equals(name, StringComparison.OrdinalIgnoreCase)
                || x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || x.Symbol.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (crypto is null)
            {
                crypto = cryptoData.Select(x => (Crypto: x, Distance: x.Name.ToUpperInvariant().LevenshteinDistance(name)))
                    .OrderBy(x => x.Distance)
                    .Where(x => x.Distance <= 2)
                    .Select(x => x.Crypto)
                    .FirstOrDefault();

                if (crypto is null)
                {
                    return new CryptoData
                    {
                        Error = Errors.NotFound
                    };
                }
                else
                {
                    crypto.IsNearest = true;
                }
            }

            return new CryptoData
            {
                Data = crypto
            };
        }

        public override async Task<PokemonData> GetPokemon(PokemonRequest request, ServerCallContext context)
        {
            await Task.Yield();
            if (!Pokemons.TryGetValue(request.Name.ToUpperInvariant(), out var poke))
            {
                return new PokemonData
                {
                    NotFound = true
                };
            }

            var data = new PokemonData.Types.Data
            {
                BaseStats = new PokemonData.Types.Data.Types.BaseStats
                {
                    Atk = poke.BaseStats.ATK,
                    Def = poke.BaseStats.DEF,
                    Spa = poke.BaseStats.SPA,
                    Hp = poke.BaseStats.HP,
                    Spd = poke.BaseStats.SPD,
                    Spe = poke.BaseStats.SPE,
                },
                Color = poke.Color,
                GenderRatio = new PokemonData.Types.Data.Types.GenderRatio
                {
                    F = poke.GenderRatio.F,
                    M = poke.GenderRatio.M,
                },
                HeightM = poke.HeightM,
                Num = poke.Num,
                Species = poke.Species,
                WeightKg = poke.WeightKg,
            };

            data.Types_.AddRange(poke.Types);
            if (poke.Evos != null)
                data.Evos.AddRange(poke.Evos);
            if (poke.EggGroups != null)
                data.EggGroups.AddRange(poke.EggGroups);
            data.Abilities.Add(poke.Abilities);

            return new PokemonData
            {
                Data = data,
            };
        }

        public override async Task<PokeabData> GetPokemonAbility(PokeabRequest request, ServerCallContext context)
        {
            await Task.Yield();
            if (!PokemonAbilities.TryGetValue(request.Ability.ToUpperInvariant(), out var ability))
            {
                return new PokeabData
                {
                    NotFound = true
                };
            }

            var response = new PokeabData
            {
                Data = new PokeabData.Types.Data
                {
                    Desc = ability.Desc,
                    Name = ability.Name,
                    Rating = ability.Rating,
                    ShortDesc = ability.ShortDesc,
                }
            };

            return response;
        }


        public override async Task<NovelData> GetNovel(NovelRequest request, ServerCallContext context)
        {
            var query = request.Query.Replace(" ", "-", StringComparison.InvariantCulture);
            if (string.IsNullOrEmpty(query))
            {
                return new NovelData
                {
                    Error = Errors.InvalidInput
                };
            }

            try
            {

                var link = "http://www.novelupdates.com/series/" + Uri.EscapeDataString(query.Replace("/", " ", StringComparison.InvariantCulture));
                link = link.ToLowerInvariant();
                using var document = await BrowsingContext.New(_browsingConfig).OpenAsync(link).ConfigureAwait(false);
                var imageElem = document.QuerySelector("div.seriesimg > img");
                if (imageElem == null)
                    return null;
                var imageUrl = ((IHtmlImageElement)imageElem).Source;

                var descElem = document.QuerySelector("div#editdescription > p");
                var desc = descElem.InnerHtml;

                var genres = document.QuerySelector("div#seriesgenre").Children
                    .Select(x => x as IHtmlAnchorElement)
                    .Where(x => x != null)
                    .Select(x => $"[{x.InnerHtml}]({x.Href})")
                    .ToArray();

                var authors = document
                    .QuerySelector("div#showauthors")
                    .Children
                    .Select(x => x as IHtmlAnchorElement)
                    .Where(x => x != null)
                    .Select(x => $"[{x.InnerHtml}]({x.Href})")
                    .ToArray();

                var score = ((IHtmlSpanElement)document
                    .QuerySelector("h5.seriesother > span.uvotes"))
                    .InnerHtml;

                var status = document
                    .QuerySelector("div#editstatus")
                    .InnerHtml;
                var title = document
                    .QuerySelector("div.w-blog-content > div.seriestitlenu")
                    .InnerHtml;

                var novel = new NovelData.Types.Info()
                {
                    Description = desc,
                    CoverUrl = imageUrl,
                    Link = link,
                    Score = score,
                    Status = status,
                    Title = title,
                };

                if (genres.Any())
                {
                    novel.Genres.AddRange(genres);
                }

                if (novel.Authors.Any())
                {
                    novel.Authors.AddRange(authors);
                }

                _log.Information("Sending novel: {Query}", query);

                return new NovelData
                {
                    Data = novel
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting novel data: {Message}", ex.Message);
                return new NovelData
                {
                    Error = Errors.NotFound
                };
            }
        }

        public override async Task<ChuckNorrisJoke> GetChuckNorrisJoke(GetChuckNorrisJokeRequest request, ServerCallContext context)
        {
            var response = await _http.GetStringAsync(new Uri("http://api.icndb.com/jokes/random/")).ConfigureAwait(false);
            return new ChuckNorrisJoke
            {
                Text = JObject.Parse(response)["value"]["joke"].ToString() + " 😆",
            };
        }

        private readonly Channel<RandomJokeData> rjChannel = Channel.CreateUnbounded<RandomJokeData>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

        public override async Task<RandomJoke> GetRandomJoke(GetRandomJokeRequest request, ServerCallContext context)
        {
            // try to read cached joke
            if (rjChannel.Reader.TryRead(out var data))
            {
                // if it succeeds, send it
                return new RandomJoke
                {
                    Source = data.GetSource(),
                    Text = data.ToString(),
                };
            }
            // if there are no cached jokes
            else
            {
                // get some
                var responseText = await _http.GetStringAsync("https://www.goodbadjokes.com/jokes.json");
                var jokeArr = JsonConvert.DeserializeObject<RandomJokeData[]>(responseText);

                // cache all but first
                for (int i = 1; i < jokeArr.Length; i++)
                {
                    await rjChannel.Writer.WriteAsync(jokeArr[i]);
                }

                // send first
                return new RandomJoke
                {
                    Source = jokeArr[0].GetSource(),
                    Text = jokeArr[0].ToString(),
                };
            }
        }

        public class RandomJokeData
        {
            public string Slug { get; set; }
            public string Joke { get; set; }
            [JsonProperty("punch_line")]
            public string Punchline { get; set; }

            public override string ToString()
            {
                return $"{Joke}\n\n{Punchline}";
            }

            public string GetSource() => $"https://www.goodbadjokes.com/jokes/{this.Slug}";
        }

        public override async Task<YomamaJoke> GetYomamaJoke(GetYomamaJokeRequest request, ServerCallContext context)
        {
            var response = await _http.GetStringAsync(new Uri("http://api.yomomma.info/")).ConfigureAwait(false);
            return new YomamaJoke
            {
                Text = JObject.Parse(response)["joke"].ToString() + " 😆"
            };
        }

        private readonly SemaphoreSlim getCryptoLock = new SemaphoreSlim(1, 1);
        private async Task<List<CryptoData.Types.Info>> InternalGetCryptoDataAsync()
        {
            await getCryptoLock.WaitAsync();
            try
            {
                var fullStrData = await _cache.GetOrAddStringAsync("nadeko:crypto_data", async (key) =>
                {
                    try
                    {
                        var strData = await _http.GetStringAsync(new Uri($"https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest?" +
                            $"CMC_PRO_API_KEY=e79ec505-0913-439d-ae07-069e296a6079" +
                            $"&start=1" +
                            $"&limit=500" +
                            $"&convert=USD"));

                        JsonConvert.DeserializeObject<CryptoResponse>(strData); // just to see if its' valid

                        return strData;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error getting crypto data: {Message}", ex.Message);
                        return default;
                    }

                }, TimeSpan.FromHours(1));

                return JsonConvert.DeserializeObject<CryptoResponse>(fullStrData)
                    .Data
                    .Select(x => new CryptoData.Types.Info
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Rank = x.Rank,
                        Symbol = x.Symbol,
                        WebsiteSlug = x.Slug,
                        MarketCap = x.Quote.Usd.Market_Cap,
                        PercentChange1H = x.Quote.Usd.Percent_Change_1h ?? "?",
                        PercentChange24H = x.Quote.Usd.Percent_Change_24h ?? "?",
                        PercentChange7D = x.Quote.Usd.Percent_Change_7d ?? "?",
                        Price = x.Quote.Usd.Price,
                        Volume24H = x.Quote.Usd.Volume_24h,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retreiving crypto data: {Message}", ex.Message);
                return new List<CryptoData.Types.Info>();
            }
            finally
            {
                getCryptoLock.Release();
            }
        }
    }
}
