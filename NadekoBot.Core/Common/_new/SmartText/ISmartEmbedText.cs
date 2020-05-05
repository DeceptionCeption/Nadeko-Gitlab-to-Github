using Discord;

namespace Ayu.Discord.Common
{
    public interface ISmartEmbedText
    {
        EmbedBuilder GetEmbed();

        string PlainText { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        string Url { get; set; }
        string Thumbnail { get; set; }
        string Image { get; set; }

        SmartTextEmbedAuthor Author { get; set; }
        SmartTextEmbedFooter Footer { get; set; }
        SmartTextEmbedField[] Fields { get; set; }

        uint Color { get; set; }
    }
}
