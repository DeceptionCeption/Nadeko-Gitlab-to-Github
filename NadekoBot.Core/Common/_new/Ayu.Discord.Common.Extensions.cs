using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ayu.Discord.Common
{
    public static class Extensions
    {
        public static async Task<IUserMessage> EditAsync(this IUserMessage message, SmartText text)
        {
            switch (text)
            {
                case ISmartEmbedText set:
                    await message.ModifyAsync(x =>
                    {
                        x.Content = set.PlainText ?? "";
                        x.Embed = set.GetEmbed().Build();
                    });
                    return message;
                case ISmartPlainText spt:
                    await message.ModifyAsync(x => x.Content = spt.Text);
                    return message;
                default:
                    throw new ArgumentException(nameof(text));
            }
        }

        public static Task<IUserMessage> SendAsync(this IUser user, SmartText text)
        {
            switch (text)
            {
                case ISmartEmbedText set: return user.SendMessageAsync(set.PlainText ?? "", embed: set.GetEmbed().Build());
                case ISmartPlainText spt: return user.SendMessageAsync(spt.Text);
                default:
                    throw new ArgumentException(nameof(text));
            }
        }

        public static Task<IUserMessage> SendAsync(this IMessageChannel channel, SmartText text)
        {
            switch (text)
            {
                case ISmartEmbedText set: return channel.SendMessageAsync(set.PlainText ?? "", embed: set.GetEmbed().Build());
                case ISmartPlainText spt: return channel.SendMessageAsync(spt.Text);
                default:
                    throw new ArgumentException(nameof(text));
            }
        }

        public static int GetTotalLength(this Embed embed) => embed.Title?.Length + embed.Author?.Name?.Length + embed.Description?.Length +
            embed.Footer?.Text?.Length + embed.Fields.Sum(f => f.Name.Length + f.Value.ToString().Length) ?? 0;

    }
}
