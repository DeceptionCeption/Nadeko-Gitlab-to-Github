using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Common;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Core.Services.Impl;
using Color = System.Drawing.Color;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class SlotCommands : GamblingSubmodule<GamblingService>
        {
            private static long _totalBet;
            private static long _totalPaidOut;

            private static readonly HashSet<ulong> _runningUsers = new HashSet<ulong>();

            //here is a payout chart
            //https://lh6.googleusercontent.com/-i1hjAJy_kN4/UswKxmhrbPI/AAAAAAAAB1U/82wq_4ZZc-Y/DE6B0895-6FC1-48BE-AC4F-14D1B91AB75B.jpg
            //thanks to judge for helping me with this

            private readonly IImageCache _images;
            private readonly ICurrencyService _cs;
            private FontProvider _fonts;
            private readonly DbService _db;

            public SlotCommands(IDataCache data, ICurrencyService cs, FontProvider fonts, DbService db)
            {
                _images = data.LocalImages;
                _cs = cs;
                _fonts = fonts;
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [OwnerOnly]
            public async Task SlotStats()
            {
                var paid = _totalPaidOut;
                var bet = _totalBet;

                if (bet <= 0)
                    bet = 1;

                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("Slot Stats")
                    .AddField(efb => efb.WithName("Total Bet").WithValue(bet.ToString()).WithIsInline(true))
                    .AddField(efb => efb.WithName("Paid Out").WithValue(paid.ToString()).WithIsInline(true))
                    .WithFooter(efb => efb.WithText($"Payout Rate: {paid * 1.0 / bet * 100:f4}%"));

                await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Slot(ShmartNumber amount)
            {
                if (!_runningUsers.Add(ctx.User.Id))
                    return;
                try
                {
                    if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                        return;
                    await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);

                    var result = await _service.SlotAsync(ctx.User.Id, amount);

                    if (result.Error != GamblingError.None)
                    {
                        if (result.Error == GamblingError.NotEnough)
                        {
                            await ReplyErrorLocalizedAsync("not_enough", Bc.BotConfig.CurrencySign);
                        }

                        return;
                    }

                    Interlocked.Add(ref _totalBet, amount);
                    Interlocked.Add(ref _totalPaidOut, result.Won);

                    long ownedAmount;
                    using (var uow = _db.GetDbContext())
                    {
                        ownedAmount = uow._context.Set<DiscordUser>().FirstOrDefault(x => x.UserId == ctx.User.Id)
                            ?.CurrencyAmount ?? 0;
                    }

                    using (var bgImage = Image.Load<Rgba32>(_images.SlotBackground, out var format))
                    {
                        var numbers = new int[3];
                        result.Rolls.CopyTo(numbers, 0);

                        bgImage.Mutate(x => x.DrawText(new TextGraphicsOptions
                            {
                                TextOptions = new TextOptions()
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    WrapTextWidth = 140,
                                }
                            }, result.Won.ToString(), _fonts.NotoSans.CreateFont(30), SixLabors.ImageSharp.Color.Red,
                            new PointF(225, 95)));

                        bgImage.Mutate(x => x.DrawText(new TextGraphicsOptions
                            {
                                TextOptions = new TextOptions()
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    WrapTextWidth = 135,
                                }
                            }, amount.ToString(), _fonts.NotoSans.CreateFont(25), SixLabors.ImageSharp.Color.Red,
                            new PointF(129, 475)));

                        bgImage.Mutate(x => x.DrawText(new TextGraphicsOptions
                            {
                                TextOptions = new TextOptions()
                                {
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    WrapTextWidth = 135,
                                }
                            }, ownedAmount.ToString(), _fonts.NotoSans.CreateFont(25), SixLabors.ImageSharp.Color.Red,
                            new PointF(325, 475)));
                        //sw.PrintLap("drew red text");

                        for (var i = 0; i < 3; i++)
                        {
                            using (var img = Image.Load(_images.SlotEmojis[numbers[i]]))
                            {
                                bgImage.Mutate(x => x.DrawImage(img, new Point(148 + 105 * i, 230), 1f));
                            }
                        }

                        var msg = GetText("better_luck");
                        if (result.Multiplier > 0)
                        {
                            if (result.Multiplier == 1)
                                msg = GetText("slot_single", Bc.BotConfig.CurrencySign, 1);
                            else if (result.Multiplier == 4)
                                msg = GetText("slot_two", Bc.BotConfig.CurrencySign, 4);
                            else if (result.Multiplier == 10)
                                msg = GetText("slot_three", 10);
                            else if (result.Multiplier == 30)
                                msg = GetText("slot_jackpot", 30);
                        }

                        using (var imgStream = bgImage.ToStream())
                        {
                            await ctx.Channel.SendFileAsync(imgStream,
                                filename: "result.png",
                                text: Format.Bold(ctx.User.ToString()) + " " + msg).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        _runningUsers.Remove(ctx.User.Id);
                    });
                }
            }
        }
    }
}