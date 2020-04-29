using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OldNadekoCommandAttribute : CommandAttribute
    {
        public OldNadekoCommandAttribute([CallerMemberName] string memberName="") : base(Localization.LoadCommand(memberName.ToLowerInvariant()).Cmd.Split(' ')[0])
        {

        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NadekoCommandAttribute : CommandAttribute
    {
        public NadekoCommandAttribute(params string[] aliases) : base(null, aliases)
        {

        }

        public NadekoCommandAttribute() : base(null, null)
        {

        }
    }
}
