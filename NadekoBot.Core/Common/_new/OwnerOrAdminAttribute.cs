using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using NadekoBot.Core.Services;
using System;
using System.Threading.Tasks;

namespace Nadeko.Bot.Common.Attributes
{
    public class OwnerOrAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var creds = services.GetService<IBotCredentials>();
            return await IsAdminOrOwnerAsync(context, creds)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Not an owner in dms nor admin in a server.");
        }

        public static async Task<bool> IsAdminOrOwnerAsync(ICommandContext ctx, IBotCredentials creds)
        {
            await Task.Yield();
            if (ctx.User is IGuildUser)
                return ((IGuildUser)ctx.User).GuildPermissions.Administrator;

            return creds.OwnerIds.Contains(ctx.User.Id);
        }
    }
}
