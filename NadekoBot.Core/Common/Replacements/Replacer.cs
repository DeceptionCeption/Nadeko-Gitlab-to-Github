using Ayu.Discord.Common;
using Ayu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NadekoBot.Common.Replacements
{
    public class Replacer
    {
        private readonly IEnumerable<(string Key, Func<string> Text)> _replacements;
        private readonly IEnumerable<(Regex Regex, Func<Match, string> Replacement)> _regex;

        public Replacer(IEnumerable<(string, Func<string>)> replacements, IEnumerable<(Regex, Func<Match, string>)> regex)
        {
            _replacements = replacements;
            _regex = regex;
        }

        public string Replace(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            foreach (var (Key, Text) in _replacements)
            {
                if (input.Contains(Key))
                    input = input.Replace(Key, Text(), StringComparison.InvariantCulture);
            }

            foreach (var item in _regex)
            {
                input = item.Regex.Replace(input, (m) => item.Replacement(m));
            }

            return input;
        }

        public void Replace(CREmbed embedData)
        {
            embedData.PlainText = Replace(embedData.PlainText);
            embedData.Description = Replace(embedData.Description);
            embedData.Title = Replace(embedData.Title);
            embedData.Thumbnail = Replace(embedData.Thumbnail);
            embedData.Image = Replace(embedData.Image);
            if (embedData.Author != null)
            {
                embedData.Author.Name = Replace(embedData.Author.Name);
                embedData.Author.IconUrl = Replace(embedData.Author.IconUrl);
            }

            if (embedData.Fields != null)
                foreach (var f in embedData.Fields)
                {
                    f.Name = Replace(f.Name);
                    f.Value = Replace(f.Value);
                }

            if (embedData.Footer != null)
            {
                embedData.Footer.Text = Replace(embedData.Footer.Text);
                embedData.Footer.IconUrl = Replace(embedData.Footer.IconUrl);
            }
        }

        public async Task<SmartPlainText> ReplaceAsync(SmartPlainText smartInput)
        {
            string input = smartInput.Text;
            if (string.IsNullOrWhiteSpace(input))
                return new SmartPlainText(input);

            foreach (var (Key, TextFunc) in _replacements)
            {
                if (input.Contains(Key))
                    input = input.Replace(Key, TextFunc(), StringComparison.InvariantCulture);
            }

            foreach (var item in _regex)
            {
                input = await RegexReplaceAsync(item.Regex, input, item.Replacement);
            }

            return new SmartPlainText(input.SanitizeMentions());
        }

        public async Task<SmartText> ReplaceAsync(SmartText text)
        {
            switch (text)
            {
                case SmartEmbedText embed:
                    return await ReplaceAsync(embed);
                case SmartPlainText plain:
                    return await ReplaceAsync(plain);
                case null:
                    return null;
                default:
                    throw new NotImplementedException(text?.GetType().FullName ?? "?!");
            }
        }

        public async Task<SmartEmbedText> ReplaceAsync(SmartEmbedText embedData)
        {
            var newEmbedData = new SmartEmbedText();
            newEmbedData.PlainText = await ReplaceAsync(embedData.PlainText);
            newEmbedData.Description = await ReplaceAsync(embedData.Description);
            newEmbedData.Title = await ReplaceAsync(embedData.Title);
            newEmbedData.Thumbnail = await ReplaceAsync(embedData.Thumbnail);
            newEmbedData.Image = await ReplaceAsync(embedData.Image);
            if (embedData.Author != null)
            {
                newEmbedData.Author = new SmartTextEmbedAuthor();
                newEmbedData.Author.Name = await ReplaceAsync(embedData.Author.Name);
                newEmbedData.Author.IconUrl = await ReplaceAsync(embedData.Author.IconUrl);
            }

            if (embedData.Fields != null)
            {
                var fields = new List<SmartTextEmbedField>();
                foreach (var f in embedData.Fields)
                {
                    var newF = new SmartTextEmbedField();
                    newF.Name = await ReplaceAsync(f.Name);
                    newF.Value = await ReplaceAsync(f.Value);
                    fields.Add(newF);
                }

                newEmbedData.Fields = fields.ToArray();
            }

            if (embedData.Footer != null)
            {
                newEmbedData.Footer = new SmartTextEmbedFooter();
                newEmbedData.Footer.Text = await ReplaceAsync(embedData.Footer.Text);
                newEmbedData.Footer.IconUrl = await ReplaceAsync(embedData.Footer.IconUrl);
            }

            newEmbedData.Color = embedData.Color;

            return newEmbedData;
        }

        private static async Task<string> RegexReplaceAsync(Regex regex, string input, Func<Match, string> replacementFn)
        {
            await Task.Yield();
            var sb = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in regex.Matches(input))
            {
                sb.Append(input, lastIndex, match.Index - lastIndex)
                  .Append(replacementFn(match));

                lastIndex = match.Index + match.Length;
            }

            sb.Append(input, lastIndex, input.Length - lastIndex);
            return sb.ToString();
        }
    }
}
