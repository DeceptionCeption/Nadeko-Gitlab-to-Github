using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using static Nadeko.Bot.Common.Kwok;
using NadekoBot.Common.Replacements;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class QuoteCommands : NadekoSubmodule
        {
            private readonly Nadeko.Microservices.Expressions.ExpressionsClient _expr;
            private readonly ReplacementBuilderService _repService;

            public QuoteCommands(Nadeko.Microservices.Expressions.ExpressionsClient expressions, ReplacementBuilderService repService)
            {
                _expr = expressions;
                _repService = repService;
            }

            [NadekoCommand("qadd", ".")]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteAdd(string trigger, [Leftover] string response)
            {
                if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(response))
                {
                    return;
                }

                var res = await Rpc(ctx, _expr.AddExpressionAsync, new AddExpresionRequest
                {
                    AuthorId = ctx.User.Id,
                    AuthorName = ctx.User.ToString(),
                    GuildId = ctx.Guild?.Id ?? 0,
                    Response = response,
                    Trigger = trigger,
                    IsQuote = true,
                });

                await ctx.Channel.SendMessageAsync(new EmbedBuilder()
                    .WithOkColor(ctx)
                    .WithTitle(GetText("new_quote"))
                    .WithDescription($"#{IntToKwok(res.ExprId)}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(response.Length > 1024 ? GetText("redacted_too_long") : response))
                    ).ConfigureAwait(false);
            }

            [NadekoCommand("qrm", "qdel", "quotedelete", "quoteremove")]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteRm(string idStr)
            {
                if (!KwokToInt(idStr, out var id))
                    return;

                var res = await Rpc(ctx, _expr.DeleteExpressionAsync, new DeleteExpressionRequest
                {
                    GuildId = ctx.Guild?.Id ?? 0,
                    Id = id,
                    IsQuote = true,
                    IsAdmin = await OwnerOrAdminAttribute.IsAdminAsync(ctx),
                    UserId = ctx.User.Id,
                });

                if (!res.Success)
                {
                    await ctx.ReplyErrorLocalizedAsync("quote_manipulation_fail");
                    return;
                }

                await ctx.Channel.SendAsync(new EmbedBuilder()
                    .WithOkColor(ctx)
                    .WithTitle(GetText("quote_deleted"))
                    .WithDescription($"#{idStr}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(res.ExprData.Trigger.TrimTo(1024)))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(res.ExprData.Response.TrimTo(1024)))).ConfigureAwait(false);
            }

            [NadekoCommand("qclear", "qclr", "quoteclear")]
            [OwnerOrAdmin]
            [RequireContext(ContextType.Guild)]
            public async Task QuotesClear(GuildContext ctx)
            {
                if (!await ctx.PromptUserConfirmAsync(new EmbedBuilder()
                    .WithPendingColor(ctx)
                    .WithDescription(GetText("quote_delete_all_confirm"))))
                {
                    return;
                }

                var res = await Rpc(ctx, _expr.DeleteAllExpressionsAsync, new DeleteAllExpressionsRequest
                {
                    GuildId = Context.Guild?.Id ?? 0,
                    IsQuote = true,
                });

                await ctx.ReplyConfirmLocalizedAsync("quote_deleted_all", Format.Bold(res.Count.ToString()));
            }

            [OldNadekoCommand("qedit")]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteEdit(string idStr, [Leftover] string response)
            {
                if (!KwokToInt(idStr, out var id))
                    return;

                var res = await Rpc(ctx, _expr.EditExpressionAsync, new EditExpressionRequest
                {
                    Id = id,
                    Response = response,
                    GuildId = Context.Guild?.Id ?? 0,
                    IsQuote = true,
                    IsAdmin = await OwnerOrAdminAttribute.IsAdminAsync(ctx),
                    UserId = ctx.User.Id,
                });

                if (!res.Success)
                {
                    await ctx.ReplyErrorLocalizedAsync("quote_manipulation_fail");
                    return;
                }

                await ctx.Channel.SendAsync(new EmbedBuilder()
                    .WithOkColor(ctx)
                    .WithTitle(GetText("quote_edited"))
                    .WithDescription($"#{idStr}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(res.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(res.Response.Length > 1024 ? GetText("redacted_too_long") : res.Response))
                    ).ConfigureAwait(false);
            }

            public class ListOptions : CommandOptions
            {
                [Option('a', "alphabetic", Default = false, HelpText = "Sort in alphabetic order.", Required = true)]
                public bool Alphabetic { get; set; }

                public override void NormalizeOptions()
                {
                }
            }

            [NadekoCommand("qlist", "qli")]
            [RequireContext(ContextType.Guild)]
            public Task QuoteList([Leftover] Options<ListOptions> options = null)
                => QuoteList(ctx, 1, options);

            [NadekoCommand("qlist", "qli")]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteList(int page = 1, [Leftover] Options<ListOptions> options = null)
            {
                var alph = options?.Data.Alphabetic ?? false;
                if (--page < 0)
                {
                    return;
                }

                var res = await Rpc(ctx, _expr.ListExpressionsAsync, new ListExpressionsRequest
                {
                    GuildId = ctx.Guild?.Id ?? 0,
                    Page = page,
                    IsQuote = true,
                    Alphabetical = alph,
                });

                var qstr = string.Join("\n", res.Data.Select(expr =>
                {
                    var str = $"`{IntToKwok(expr.Id)}` {expr.Trigger.TrimTo(50)}";
                    //if (expr.AutoDelete)
                    //{
                    //    str = "🗑" + str;
                    //}
                    //if (expr.DmResponse)
                    //{
                    //    str = "📪" + str;
                    //}
                    return str;
                }));

                if (string.IsNullOrWhiteSpace(qstr))
                    qstr = GetText("quote_no_found");

                var eb = new EmbedBuilder()
                    .WithOkColor(ctx)
                    .WithTitle(GetText("quote_list"))
                    .WithDescription(qstr)
                    .WithFooter(GetText("page", page + 1));

                await ctx.Channel.SendAsync(eb);
            }

            [OldNadekoCommand("qshow")]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteShow(string idStr)
            {
                if (!KwokToInt(idStr, out var id))
                    return;

                var res = await Rpc(ctx, _expr.GetExpressionAsync, new GetExpressionRequest
                {
                    Id = id,
                    GuildId = Context.Guild?.Id ?? 0,
                    IsQuote = true,
                });

                if (!res.Success)
                {
                    await ctx.ReplyErrorLocalizedAsync("quote_no_found_id");
                    return;
                }

                var data = res.ExprData;

                await ShowQuoteData(ctx, data);
            }

            private async Task ShowQuoteData(ExprData data)
            {
                await ctx.Channel.SendAsync(new EmbedBuilder()
                    .WithOkColor(ctx)
                    .WithTitle(GetText("quote"))
                    .WithDescription($"#{IntToKwok(data.Id)}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(data.Trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(data.Response.Length > 1000
                        ? GetText("redacted_too_long")
                        : Format.Escape(data.Response)))
                    .WithFooter(GetText("created_by", $"{data.AuthorName} ({data.AuthorId})"))
                    ).ConfigureAwait(false);
            }

            // todo 3.2 add ..., .. and a footer which will say those 2 are going ot get removed
            [NadekoCommand("qp", "..")]
            [RequireContext(ContextType.Guild)]
            public async Task QuotePrint(string trigger)
            {
                var res = await Rpc(ctx, _expr.GetRandomExpressionAsync, new GetRandomExpressionRequest
                {
                    Trigger = trigger,
                    GuildId = Context.Guild?.Id ?? 0,
                    IsQuote = true,
                });

                if (!res.Success)
                {
                    return;
                }

                var rep = _repService.CreateReplacementBuilder()
                    .WithDefault(ctx)
                    .Build();

                await ctx.Channel.SendAsync(await rep.ReplaceAsync(SmartText.CreateFrom(res.ExprData.Response)));
            }

            [OldNadekoCommand("qpi")]
            [RequireContext(ContextType.Guild)]
            public async Task QuotePrintId(string idStr)
            {
                if (!KwokToInt(idStr, out var id))
                    return;

                var res = await Rpc(ctx, _expr.GetExpressionAsync, new GetExpressionRequest
                {
                    GuildId = Context.Guild?.Id ?? 0,
                    IsQuote = true,
                    Id = id,
                });

                if (!res.Success)
                {
                    return;
                }

                var rep = _repService.CreateReplacementBuilder()
                    .WithDefault(ctx)
                    .Build();

                await ctx.Channel.SendAsync(await rep.ReplaceAsync(SmartText.CreateFrom(res.ExprData.Response)));
            }
            public class SearchOptions : CommandOptions
            {
                [Option('t', "trigger", Required = false, Default = "",
                    HelpText = "Part of the trigger will have to match this string.")]
                public string Trigger { get; set; }

                [Option('r', "response", Required = false, Default = "",
                    HelpText = "Part of the response will have to match this string.")]
                public string Response { get; set; }

                [Option('n', "no-print", Required = false, Default = false,
                    HelpText = "Show the raw response, instead of the print version.")]
                public bool NoPrint { get; set; }

                public override void NormalizeOptions()
                {

                }
            }

            [NadekoCommand("qsrch", "qsearch")]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearch([Leftover] Options<SearchOptions> options)
            {
                if (string.IsNullOrWhiteSpace(options.Data.Trigger) &&
                    string.IsNullOrWhiteSpace(options.Data.Response))
                {
                    return;
                }

                var res = await Rpc(ctx, _expr.FindExpressionAsync, new FindExpressionRequest
                {
                    GuildId = Context.Guild?.Id ?? 0,
                    IsQuote = true,
                    Trigger = options.Data.Trigger,
                    Response = options.Data.Response,
                });

                if (!res.Success)
                {
                    return;
                }

                if (options.Data.NoPrint)
                {
                    await ShowQuoteData(ctx, res.ExprData);
                }
                else
                {
                    var rep = _repService.CreateReplacementBuilder()
                        .WithDefault(ctx)
                        .Build();

                    await ctx.Channel.SendAsync(await rep.ReplaceAsync(SmartText.CreateFrom(res.ExprData.Response)));
                }
            }
        }
    }
}
