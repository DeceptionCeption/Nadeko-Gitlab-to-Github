using ExpressionsService.Database;
using ExpressionsService.Database.Models;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Nadeko.Microservices;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;
using Ayu.Common;
using System.Collections.Generic;

namespace ExpressionsService
{
    public class ExpressionsService : Expressions.ExpressionsBase
    {
        private readonly ExpressionsDb _exprDb;
        private readonly NadekoRandom _rng;

        private readonly Dictionary<ulong, IList<Expression>> _expressions;
        private readonly object exprLock = new object();

        public ExpressionsService(ExpressionsDb exprDb)
        {
            _exprDb = exprDb;
            _rng = new NadekoRandom();
            // todo gotta check some other way whether the migration is already done
            if (File.Exists("data/NadekoBot.db"))
            {
                var conn = new SqliteConnection($"Data Source=data/NadekoBot.db;Mode=readonly");
                conn.Open();
                try
                {
                    MigrateExpressions(conn);
                }
                finally
                {
                    conn.Close();
                }
            }
            var ctx = _exprDb.GetDbContext();

            _expressions = ctx
                .Expressions
                .Where(x => !x.IsQuote)
                .AsEnumerable()
                .GroupBy(x => x.GuildId, x => x)
                .ToDictionary(x => x.Key, x => (IList<Expression>)x.AsEnumerable().ToList());
        }

        private void MigrateExpressions(SqliteConnection conn)
        {
            Log.Information("Migrating quotes data...");


            using var uow = _exprDb.GetDbContext();
            uow.Database.ExecuteSqlRaw("delete from expressions.expressions;");

            using var com = conn.CreateCommand();
            com.CommandText = $@"SELECT AuthorId, AuthorName, GuildId, Keyword, Text FROM Quotes;";
            using (var reader = com.ExecuteReader())
            {
                while (reader.Read())
                {
                    var authorId = (ulong)reader.GetInt64(0);
                    var authorName = reader.GetString(1);
                    var guildId = (ulong)reader.GetInt64(2);
                    var keyword = reader.GetString(3);
                    var text = reader.GetString(4);

                    uow.Expressions.Add(new Expression
                    {
                        IsQuote = true,
                        AuthorId = authorId,
                        AuthorName = authorName,
                        GuildId = guildId,
                        Trigger = keyword,
                        Response = text,
                    });
                }
            }


            Log.Information("Migrating customreactions data...");
            com.CommandText = $@"SELECT GuildId, Trigger, Response, AutoDeleteTrigger, DmResponse FROM CustomReactions
WHERE IsRegex = 0 AND
    OwnerOnly = 0 AND
    ContainsAnywhere = 0;";
            using (var reader = com.ExecuteReader())
            {
                while (reader.Read())
                {
                    var authorId = 0ul; // unknown, data doesn't exist in 2.x
                    var authorName = "Unknown"; // unknown, data doesn't exist in 2.x
                    var guildId = reader.IsDBNull(0) ? 0 : (ulong)reader.GetInt64(0);
                    var trigger = reader.GetString(1);
                    var response = reader.GetString(2);
                    var ad = reader.GetBoolean(3);
                    var dm = reader.GetBoolean(4);

                    uow.Expressions.Add(new Expression
                    {
                        IsQuote = false,
                        AuthorId = authorId,
                        AuthorName = authorName,
                        GuildId = guildId,
                        Trigger = trigger,
                        Response = response,
                        AutoDelete = ad,
                        DirectMessage = dm,
                    });
                }
            }

            uow.SaveChanges();
        }

        private ExprData ModelToReply(Expression expr) => new ExprData
        {
            Id = expr.Id,
            Trigger = expr.Trigger,
            AutoDelete = expr.AutoDelete,
            DmResponse = expr.DirectMessage,
            AuthorId = expr.AuthorId,
            AuthorName = expr.AuthorName,
            Response = expr.Response,
            ContainsAnywhere = expr.ContainsAnywhere,
        };

        public override async Task<AddExpressionReply> AddExpression(AddExpresionRequest request, ServerCallContext context)
        {
            try
            {
                var trigger = request.Trigger.ToLowerInvariant().Trim();
                using var uow = _exprDb.GetDbContext();
                var expr = new Expression
                {
                    AuthorId = request.AuthorId,
                    AuthorName = request.AuthorName,
                    GuildId = request.GuildId,
                    Response = request.Response,
                    Trigger = trigger,
                    IsQuote = request.IsQuote,
                };

                uow.Expressions.Add(expr);

                await uow.SaveChangesAsync();
                if (!expr.IsQuote)
                {
                    lock (exprLock)
                    {
                        if (!_expressions.TryGetValue(request.GuildId, out var exprs))
                            _expressions[request.GuildId] = new List<Expression>() { expr };
                        else
                            exprs.Add(expr);
                    }
                }

                return new AddExpressionReply
                {
                    ExprId = expr.Id,
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AddExpression: {Message}", ex.Message);
                throw;
            }
        }

        // target placeholder
        private const string PH_TARGET = "%target%";
        // this is used with exprca (contains anywhere) which matches the keyword anywhere in the string (if its surrounded by spaces)
        // it will return data before the matched text, opposed to target which will
        private const string PH_BEFORE = "%before%";
        // same as target
        private const string PH_AFTER = "%after%";
        private (Expression Expr, string NewResponse) FindExpression(ulong guildId, string input, ulong botId)
        {
            string GetResponse(Expression original, string before = "", string after = "")
                => original
                    .Response
                    .Replace(PH_BEFORE, before)
                    .Replace(PH_AFTER, after)
                    .Replace(PH_TARGET, after);

            if (guildId != 236275461590745088)
                return default;
            lock (exprLock)
            {
                if (_expressions.TryGetValue(guildId, out var exprs))
                {
                    exprs.ShuffleList(_rng);
                    foreach (var expr in exprs)
                    {
                        var trigger = expr.Trigger;
                        trigger = trigger
                            .Replace("%bot.mention%", $"<@{botId}>")
                            .Replace("%mention%", $"<@{botId}>");
                        var triggerLength = expr.Trigger.Length;
                        var index = 0;

                        if (input == trigger)
                        {
                            return (expr, GetResponse(expr));
                        }
                        else if ((expr.Response.Contains(PH_AFTER) || expr.Response.Contains(PH_TARGET)) && input.StartsWith(trigger + " "))
                        {
                            return (expr, GetResponse(expr, string.Empty, input[(triggerLength + 1)..]));
                        }
                        else if (expr.ContainsAnywhere)
                        {
                            if (input.StartsWith(trigger + " "))
                            {
                                return (expr, GetResponse(expr, string.Empty, input[(triggerLength + 1)..]));
                            }
                            else if (input.EndsWith(" " + trigger))
                            {
                                return (expr, GetResponse(expr, input[0..^(triggerLength + 1)], string.Empty));
                            }
                            else if ((index = input.IndexOf($" {trigger} ")) != -1)
                            {
                                return (expr, GetResponse(expr, input[0..index], input[(index + triggerLength + 2)..]));
                            }
                        }
                    }
                }

                // if guild custom reaction not found, try finding a global one
                if (guildId != 0)
                    return FindExpression(0, input, botId);
            }

            return (null, string.Empty);
        }

        public override async Task<QueryForExpressionReply> QueryForExpression(QueryForExpressionRequest request, ServerCallContext context)
        {
            var content = request.Content
                .ToLowerInvariant()
                .Trim()
                .Replace("<@!", "<@");

            lock (exprLock)
            {
                var (expr, res) = FindExpression(request.GuildId, content, request.BotId);

                if (expr is null || expr.Response == "-")
                {
                    return new QueryForExpressionReply
                    {
                        Fail = true,
                    };
                }
                else
                {
                    var data = ModelToReply(expr);
                    data.Response = res;
                    return new QueryForExpressionReply
                    {
                        Data = data
                    };
                }
            }
        }

        public override async Task<ListExpressionsReply> ListExpressions(ListExpressionsRequest request, ServerCallContext context)
        {
            using var uow = _exprDb.GetDbContext();
            var query = uow.Expressions
                .Where(x => x.GuildId == request.GuildId && x.IsQuote == request.IsQuote);

            if (request.Alphabetical)
                query = query.OrderBy(x => x.Trigger);
            else
                query = query.OrderBy(x => x.Id);

            query = query.Skip(20 * request.Page)
                .Take(20);

            var exprs = await query.ToListAsync();

            var res = new ListExpressionsReply();

            res.Data.Add(exprs.Select(x => ModelToReply(x)));

            return res;
        }

        public override async Task<DeleteExpressionReply> DeleteExpression(DeleteExpressionRequest request, ServerCallContext context)
        {
            await Task.Yield();

            using var uow = _exprDb.GetDbContext();
            var expr = uow.Expressions.FromSqlInterpolated($@"
DELETE FROM expressions.expressions
WHERE guildid={request.GuildId} AND id={request.Id} AND isquote={request.IsQuote}
    AND ((NOT isquote) OR {request.IsAdmin} OR {request.UserId}=authorid)
RETURNING *;")
                .AsEnumerable()
                .FirstOrDefault();

            if (expr is null)
            {
                return new DeleteExpressionReply
                {
                    Success = false,
                };
            }

            if (!expr.IsQuote)
            {
                lock (exprLock)
                {
                    if (_expressions.TryGetValue(request.GuildId, out var data))
                    {
                        for (var i = 0; i < data.Count; ++i)
                        {
                            var cur = data[i];
                            if (cur.Id != expr.Id)
                            {
                                continue;
                            }
                            else
                            {
                                data.RemoveAt(i);
                                if (data.Count == 0)
                                    _expressions.Remove(request.GuildId);
                                break;
                            }
                        }
                    }
                }
            }

            return new DeleteExpressionReply
            {
                Success = true,
                ExprData = ModelToReply(expr),
            };
        }

        public override async Task<EditExpressionReply> EditExpression(EditExpressionRequest request, ServerCallContext context)
        {
            using var uow = _exprDb.GetDbContext();
            var expr = (await uow.Expressions.FromSqlInterpolated($@"
UPDATE expressions.expressions
SET response={request.Response}
WHERE guildid={request.GuildId} AND id={request.Id} AND isquote={request.IsQuote}
    AND ((NOT isquote) OR {request.IsAdmin} OR {request.UserId}=authorid)
RETURNING *")
                .ToListAsync())
                .FirstOrDefault();

            if (expr is null)
            {
                return new EditExpressionReply
                {
                    Success = false,
                };
            }

            if (!expr.IsQuote)
            {
                lock (exprLock)
                {
                    if (_expressions.TryGetValue(request.GuildId, out var exprs))
                    {
                        for (int i = 0; i < exprs.Count; i++)
                        {
                            if (exprs[i].Id == request.Id)
                            {
                                exprs[i] = expr;
                                break;
                            }
                        }
                    }
                }
            }

            return new EditExpressionReply
            {
                Response = expr.Response,
                Success = true,
                Trigger = expr.Trigger,
            };
        }

        public override async Task<GetExpressionReply> GetExpression(GetExpressionRequest request, ServerCallContext context)
        {
            using var uow = _exprDb.GetDbContext();
            var expr = await uow.Expressions
                .FirstOrDefaultAsync(x => x.GuildId == request.GuildId
                    && x.Id == request.Id
                    && x.IsQuote == request.IsQuote);

            if (expr is null)
            {
                return new GetExpressionReply
                {
                    Success = false,
                };
            }

            return new GetExpressionReply
            {
                Success = true,
                ExprData = ModelToReply(expr),
            };
        }

        public override async Task<ExpressionBehaviorReply> SetExpresssionBehavior(ExpressionBehaviorRequest request, ServerCallContext context)
        {
            try
            {
                using var uow = _exprDb.GetDbContext();
                var expr = await uow.Expressions.FirstOrDefaultAsync(x => x.Id == request.Id && x.GuildId == request.GuildId);

                if (expr is null)
                {
                    return new ExpressionBehaviorReply
                    {
                        Success = false,
                    };
                }

                var newVal = request.Behavior switch
                {
                    ExprBehavior.AutoDelete => expr.AutoDelete = !expr.AutoDelete,
                    ExprBehavior.DirectMessage => expr.DirectMessage = !expr.DirectMessage,
                    ExprBehavior.ContainsAnywhere => expr.ContainsAnywhere = !expr.ContainsAnywhere,
                    _ => false
                };

                await uow.SaveChangesAsync();

                if (!expr.IsQuote)
                {
                    lock (exprLock)
                    {
                        if (_expressions.TryGetValue(request.GuildId, out var exprs))
                        {
                            for (int i = 0; i < exprs.Count; i++)
                            {
                                if (exprs[i].Id == request.Id)
                                {
                                    exprs[i] = expr;
                                    break;
                                }
                            }
                        }
                    }
                }

                return new ExpressionBehaviorReply
                {
                    Success = true,
                    IsEnabled = newVal,
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting expr behavior: {Message}", ex.Message);
                throw;
            }
        }

        public override async Task<GetRandomExpressionReply> GetRandomExpression(GetRandomExpressionRequest request, ServerCallContext context)
        {
            var trigger = request.Trigger.Trim().ToLowerInvariant();

            using var uow = _exprDb.GetDbContext();
            var expr = await uow.Expressions
                .Where(x => x.GuildId == request.GuildId
                    && x.Trigger == trigger
                    && x.IsQuote == true)
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (expr is null)
            {
                return new GetRandomExpressionReply
                {
                    Success = false,
                };
            }

            return new GetRandomExpressionReply
            {
                Success = true,
                ExprData = ModelToReply(expr),
            };
        }

        public override async Task<DeleteAllExpressionsReply> DeleteAllExpressions(DeleteAllExpressionsRequest request, ServerCallContext context)
        {
            using var uow = _exprDb.GetDbContext();
            var deleteCount = await uow.Database.ExecuteSqlInterpolatedAsync($@"
DELETE from expressions.expressions
WHERE guildid={request.GuildId} AND isquote={request.IsQuote};
");
            return new DeleteAllExpressionsReply
            {
                Count = deleteCount
            };
        }

        public override async Task<FindExpressionReply> FindExpression(FindExpressionRequest request, ServerCallContext context)
        {
            var trig = request.Trigger.Trim().ToLowerInvariant();
            var res = request.Response.Trim().ToLowerInvariant();

            using var uow = _exprDb.GetDbContext();
            var expr = await uow.Expressions
                .Where(x => request.GuildId == x.GuildId && request.IsQuote == x.IsQuote)
                .Where(x => string.IsNullOrWhiteSpace(trig) || x.Trigger.Contains(trig))
                .Where(x => string.IsNullOrWhiteSpace(res) || x.Response.Contains(res))
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (expr is null)
            {
                return new FindExpressionReply
                {
                    Success = false
                };
            }

            return new FindExpressionReply
            {
                Success = true,
                ExprData = ModelToReply(expr),
            };
        }
    }
}

