using Ayu.Discord.Common;
using CommandLine;
using Nadeko.Bot.Common.Attributes;
using Ayu.Common;
using Nadeko.Microservices;
using System.Linq;
using System.Threading.Tasks;
using static Nadeko.Bot.Common.Kwok;
using NadekoBot.Modules;
using NadekoBot.Common.Replacements;
using NadekoBot.Common.Attributes;
using Discord;
using NadekoBot.Extensions;
using NadekoBot.Core.Common;
using Discord.Commands;

namespace Nadeko.Bot.Modules.Expressions
{
    public partial class Expressions : NadekoTopLevelModule
    {
        public const string ActualExpressionModuleName = "ACTUALCUSTOMREACTIONS";

        private readonly Microservices.Expressions.ExpressionsClient _expr;
        private readonly ReplacementBuilderService _repService;

        public Expressions(Microservices.Expressions.ExpressionsClient expr, ReplacementBuilderService repService)
        {
            _isNew = true;
            _expr = expr;
            _repService = repService;
        }

        [NadekoCommand("ea", "acr")]
        [OwnerOrAdmin]
        public async Task ExprAdd(string trigger, [Leftover] string response)
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
            });

            await ctx.Channel.SendAsync(
                new EmbedBuilder()
                    .WithOkColor(ctx)
                    .WithTitle(GetText("new_expr"))
                    .WithDescription($"#{IntToKwok(res.ExprId)}")
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(trigger))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(response.Length > 1024 ? GetText("redacted_too_long") : response))
            ).ConfigureAwait(false);
        }

        [NadekoCommand("er", "ed", "exprdel", "dcr")]
        [OwnerOrAdmin]
        public async Task ExprRm(string idStr)
        {
            if (!KwokToInt(idStr, out var id))
                return;

            var res = await Rpc(ctx, _expr.DeleteExpressionAsync, new DeleteExpressionRequest
            {
                GuildId = ctx.Guild?.Id ?? 0,
                Id = id,
                IsAdmin = true,
                UserId = ctx.User.Id,
            });

            if (!res.Success)
            {
                await ReplyErrorLocalizedAsync("expr_no_found_id");
                return;
            }

            await ctx.Channel.SendAsync(new EmbedBuilder()
                .WithOkColor(ctx)
                .WithTitle(GetText("expr_deleted"))
                .WithDescription($"#{idStr}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(res.ExprData.Trigger.TrimTo(1024)))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(res.ExprData.Response.TrimTo(1024)))).ConfigureAwait(false);
        }

        [NadekoCommand("ec", "crclear")]
        [OwnerOrAdmin]
        public async Task ExprClear()
        {
            if (!await PromptUserConfirmAsync(new EmbedBuilder()
                .WithPendingColor(ctx)
                .WithDescription(GetText("expr_delete_all_confirm"))))
            {
                return;
            }

            var res = await Rpc(ctx, _expr.DeleteAllExpressionsAsync, new DeleteAllExpressionsRequest
            {
                GuildId = ctx.Guild?.Id ?? 0,
            });

            await ReplyConfirmLocalizedAsync("expr_deleted_all", Format.Bold(res.Count.ToString()));
        }

        [NadekoCommand("ee", "ecr")]
        [OwnerOrAdmin]
        public async Task ExprEdit(string idStr, [Leftover] string response)
        {
            if (!KwokToInt(idStr, out var id))
                return;

            var res = await Rpc(ctx, _expr.EditExpressionAsync, new EditExpressionRequest
            {
                Id = id,
                Response = response,
                GuildId = ctx.Guild?.Id ?? 0,
                IsAdmin = true,
                UserId = ctx.User.Id,
            });

            if (!res.Success)
            {
                await ReplyErrorLocalizedAsync("expr_no_found_id");
                return;
            }

            await ctx.Channel.SendAsync(new EmbedBuilder()
                .WithOkColor(ctx)
                .WithTitle(GetText("expr_edited"))
                .WithDescription($"#{idStr}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(res.Trigger))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(res.Response.Length > 1024 ? GetText("redacted_too_long") : res.Response))
                ).ConfigureAwait(false);
        }

        public class ListOptions : INadekoCommandOptions
        {
            [Option('a', "alphabetic", Default = false, HelpText = "Sort in alphabetic order.", Required = true)]
            public bool Alphabetic { get; set; }

            public void NormalizeOptions()
            {
            }
        }

        // todo 3.1 .exprli all
        //[NadekoCommand("el", "eli", "exprli")]
        //public Task ExprList([AyuConst("all")] string _)
        //{
        //    var customReactions = Rpc(_expr.GetAllExpressionsAsync, new GetAll)

        //    if (customReactions == null || !customReactions.Any())
        //    {
        //        await ReplyErrorLocalizedAsync("no_found").ConfigureAwait(false);
        //        return;
        //    }

        //    using (var txtStream = await customReactions.GroupBy(cr => cr.Trigger)
        //                                                .OrderBy(cr => cr.Key)
        //                                                .Select(cr => new { Trigger = cr.Key, Responses = cr.Select(y => new { id = y.Id, text = y.Response }).ToList() })
        //                                                .ToJson()
        //                                                .ToStream()
        //                                                .ConfigureAwait(false))
        //    {

        //        if (ctx.Guild == null) // its a private one, just send back
        //            await ctx.Channel.SendFileAsync(txtStream, "customreactions.txt", GetText("list_all")).ConfigureAwait(false);
        //        else
        //            await((IGuildUser)ctx.User).SendFileAsync(txtStream, "customreactions.txt", GetText("list_all"), false).ConfigureAwait(false);
        //    }
        //}

        [NadekoCommand("el", "eli", "exprli", "lcr")]
        public Task ExprList([Leftover] params string[] args)
            => ExprList(1, args);

        [NadekoCommand("el", "eli", "exprli", "lcr")]
        public async Task ExprList(int page = 1, [Leftover] params string[] args)
        {
            var (opts, _) = OptionsParser.ParseFrom(new ListOptions(), args);
            if (--page < 0)
            {
                return;
            }

            var res = await Rpc(ctx, _expr.ListExpressionsAsync, new ListExpressionsRequest
            {
                GuildId = ctx.Guild?.Id ?? 0,
                Page = page,
                Alphabetical = opts.Alphabetic,
            });

            var qstr = string.Join("\n", res.Data.Select(expr =>
            {
                var str = $"`{IntToKwok(expr.Id)}` {expr.Trigger.TrimTo(50)}";
                if (expr.AutoDelete)
                {
                    str = "\\✗  " + str;
                }
                else
                {
                    str = "◾ " + str;
                }
                if (expr.DmResponse)
                {
                    str = $"{"\\✉ ",-6}" + str;
                }
                else
                {
                    str = "◾ " + str;
                }
                if (expr.ContainsAnywhere)
                {
                    str = $"{" \\🗯  ",-6}" + str;
                }
                else
                {
                    str = "◾ " + str;
                }
                return str;
            }));

            if (string.IsNullOrWhiteSpace(qstr))
                qstr = GetText("expr_no_found");

            var eb = new EmbedBuilder()
                .WithOkColor(ctx)
                .WithTitle(GetText("expr_list"))
                .WithDescription(qstr)
                .WithFooter(GetText("page", page + 1));

            await ctx.Channel.SendAsync(eb);
        }

        [NadekoCommand("eshow", "scr")]
        public async Task ExprShow(string idStr)
        {
            if (!KwokToInt(idStr, out var id))
                return;

            var res = await Rpc(ctx, _expr.GetExpressionAsync, new GetExpressionRequest
            {
                Id = id,
                GuildId = ctx.Guild?.Id ?? 0,
            });

            if (!res.Success)
            {
                await ReplyErrorLocalizedAsync("expr_no_found_id");
                return;
            }

            var data = res.ExprData;

            await ShowExpressionData(data);
        }

        private async Task ShowExpressionData(ExprData data)
        {
            await ctx.Channel.SendAsync(new EmbedBuilder()
                .WithOkColor(ctx)
                .WithTitle(GetText("expr"))
                .WithDescription($"#{IntToKwok(data.Id)}")
                .AddField(efb => efb.WithName(GetText("trigger")).WithValue(data.Trigger))
                .AddField(efb => efb.WithName(GetText("response")).WithValue(data.Response.Length > 1000
                    ? GetText("redacted_too_long")
                    : Format.Escape(data.Response)))
                .WithFooter(GetText("created_by", $"{data.AuthorName} ({data.AuthorId})"))
                ).ConfigureAwait(false);
        }

        [NadekoCommand("eca", "crca")]
        [OwnerOrAdmin]
        public Task ExprCa(string idStr)
            => InternalExprBehavior(ExprBehavior.ContainsAnywhere, idStr);

        [NadekoCommand("ead", "crad")]
        [OwnerOrAdmin]
        public Task ExprAd(string idStr)
            => InternalExprBehavior(ExprBehavior.AutoDelete, idStr);

        [NadekoCommand("edm", "crdm")]
        [OwnerOrAdmin]
        public Task ExprDm(string idStr)
            => InternalExprBehavior(ExprBehavior.DirectMessage, idStr);
        public class SearchOptions : INadekoCommandOptions
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

            public void NormalizeOptions()
            {

            }
        }

        [NadekoCommand("esearch")]
        public async Task ExprSearch([Leftover] params string[] args)
        {
            var (options, _) = OptionsParser.ParseFrom(new SearchOptions(), args);
            if (string.IsNullOrWhiteSpace(options.Trigger) &&
                string.IsNullOrWhiteSpace(options.Response))
            {
                return;
            }

            var res = await Rpc(ctx, _expr.FindExpressionAsync, new FindExpressionRequest
            {
                GuildId = ctx.Guild?.Id ?? 0,
                Trigger = options.Trigger,
                Response = options.Response,
            });

            if (!res.Success)
            {
                return;
            }

            if (options.NoPrint)
            {
                await ShowExpressionData(res.ExprData);
            }
            else
            {
                var rep = _repService.CreateReplacementBuilder()
                    .WithDefault(ctx)
                    .Build();

                await ctx.Channel.SendAsync(await rep.ReplaceAsync(SmartText.CreateFrom(res.ExprData.Response)));
            }
        }

        private async Task InternalExprBehavior(ExprBehavior beh, string idStr)
        {
            if (!KwokToInt(idStr, out var id))
                return;

            var res = await Rpc(ctx, _expr.SetExpresssionBehaviorAsync, new ExpressionBehaviorRequest
            {
                Id = id,
                Behavior = beh,
                GuildId = ctx.Guild?.Id ?? 0,
            });

            if (!res.Success)
            {
                await ReplyErrorLocalizedAsync("expr_no_found_id");
                return;
            }

            if (res.IsEnabled)
            {
                await ReplyConfirmLocalizedAsync("option_enabled", Format.Code(beh.ToString()), Format.Code(idStr)).ConfigureAwait(false);
            }
            else
            {
                await ReplyConfirmLocalizedAsync("option_disabled", Format.Code(beh.ToString()), Format.Code(idStr)).ConfigureAwait(false);
            }
        }
    }
}
