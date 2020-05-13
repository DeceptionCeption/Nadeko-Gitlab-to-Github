//using AngleSharp;
//using AngleSharp.Html.Dom;
//using Discord;
//using Discord.WebSocket;
//using NadekoBot.Common.Replacements;
//using Ayu.Common;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace NadekoBot.Modules.CustomReactions.Extensions
//{
//    public static class Extensions
//    {
//        private static readonly Regex imgRegex = new Regex("%(img|image):(?<tag>.*?)%", RegexOptions.Compiled);

//        private static Dictionary<Regex, Func<Match, Task<string>>> regexPlaceholders { get; } = new Dictionary<Regex, Func<Match, Task<string>>>()
//        {
//            { imgRegex, async (match) => {
//                var tag = match.Groups["tag"].ToString();
//                if(string.IsNullOrWhiteSpace(tag))
//                    return "";

//                var fullQueryLink = $"http://imgur.com/search?q={ tag }";
//                var config = Configuration.Default.WithDefaultLoader();
//                using(var document = await BrowsingContext.New(config).OpenAsync(fullQueryLink).ConfigureAwait(false))
//                {
//                    var elems = document.QuerySelectorAll("a.image-list-link").ToArray();

//                    if (!elems.Any())
//                        return "";

//                    var img = (elems.ElementAtOrDefault(new NadekoRandom().Next(0, elems.Length))?.Children?.FirstOrDefault() as IHtmlImageElement);

//                    if (img?.Source == null)
//                        return "";

//                    return " " + img.Source.Replace("b.", ".", StringComparison.InvariantCulture) + " ";
//                }
//            } }
//        };

//        private static string ResolveTriggerString(this string str, IUserMessage ctx, DiscordSocketClient client)
//        {
//            var rep = new ReplacementBuilder()
//                .WithUser(ctx.Author)
//                .WithMention(client)
//                .Build();

//            str = rep.Replace(str.ToLowerInvariant());

//            return str;
//        }

//        public static WordPosition GetWordPosition(this string str, string word)
//        {
//            var wordIndex = str.IndexOf(word, StringComparison.InvariantCulture);
//            if (wordIndex == -1)
//                return WordPosition.None;

//            if (wordIndex == 0)
//            {
//                if (word.Length < str.Length && str.isValidWordDivider(word.Length))
//                    return WordPosition.Start;
//            }
//            else if ((wordIndex + word.Length) == str.Length)
//            {
//                if (str.isValidWordDivider(wordIndex - 1))
//                    return WordPosition.End;
//            }
//            else if (str.isValidWordDivider(wordIndex - 1) && str.isValidWordDivider(wordIndex + word.Length))
//                return WordPosition.Middle;

//            return WordPosition.None;
//        }

//        private static bool isValidWordDivider(this string str, int index)
//        {
//            var ch = str[index];
//            if (ch >= 'a' && ch <= 'z')
//                return false;
//            if (ch >= 'A' && ch <= 'Z')
//                return false;

//            return true;
//        }
//    }

//    public enum WordPosition
//    {
//        None,
//        Start,
//        Middle,
//        End,
//    }
//}
