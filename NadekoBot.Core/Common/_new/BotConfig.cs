using Nadeko.Common.Yml;
using System.Collections.Generic;

// todo 3.1 str hangman_types

namespace Nadeko.Bot.Common
{
    public struct BotConfig : IInitializable
    {
        public void Initialize()
        {
            Version = 1;
            var color = new ColorConfig();
            color.Initialize();
            Color = color;
            DefaultLocale = "en-US";
            ConsoleOutputType = ConsoleOutputType.Simple;
            CheckForUpdates = UpdateCheckType.Release;
            CheckUpdateInterval = 3;
            ForwardMessages = true;
            ForwardToAllOwners = true;
            DmHelpText = @"{""description"": ""Type `%prefix%h` for help.""}";
            HelpText = @"{
  ""title"": ""To invite me to your server, use this link"",
  ""description"": ""https://discordapp.com/oauth2/authorize?client_id={0}&scope=bot&permissions=66186303"",
  ""color"": 53380,
  ""thumbnail"": ""https://i.imgur.com/nKYyqMK.png"",
  ""fields"": [
    {
      ""name"": ""Useful help commands"",
      ""value"": ""`%prefix%modules` Lists all bot modules.
`%prefix%h CommandName` Shows some help about a specific command.
`%prefix%commands ModuleName` Lists all commands in a module."",
      ""inline"": false
    },
    {
      ""name"": ""List of all Commands"",
      ""value"": ""https://nadeko.bot/commands"",
      ""inline"": false
    },
    {
      ""name"": ""Nadeko Support Server"",
      ""value"": ""https://discord.nadeko.bot/ "",
      ""inline"": true
    }
  ]
}";
            var blocked = new BlockedConfig();
            blocked.Initialize();
            Blocked = blocked;
            Prefix = ".";
            PrefixIsSuffix = false;
            RotateStatuses = false;
            PatreonCurrencyPerCent = 1;
        }

        [Comment(@"DO NOT CHANGE")]
        public int Version { get; set; }

        [Comment(@"Most commands, when executed, have a small colored line
next to the response. The color depends whether the command
is completed, errored or in progress (pending)
Color settings below are for the color of those lines.")]
        public ColorConfig Color { get; set; }
        [Comment("Default bot language. It has to be in the list of supported languages (.langli)")]
        public string DefaultLocale { get; internal set; }
        [Comment(@"Style in which executed commands will show up in the console.
Allowed values: Simple, Normal, None")]
        public ConsoleOutputType ConsoleOutputType { get; set; }

        [Comment(@"For what kind of updates will the bot check.
Allowed values: Release, Commit, None")]
        public UpdateCheckType CheckForUpdates { get; set; }

        [Comment(@"How often will the bot check for updates, in hours")]
        public int CheckUpdateInterval { get; set; }

        [Comment(@"Do you want any messages sent by users in Bot's DM to be forwarded to the owner(s)?")]
        public bool ForwardMessages { get; set; }

        [Comment(@"Do you want the message to be forwarded only to the first owner specified in the list of owners (in creds.yml),
or all owners? (this might cause the bot to lag if there's a lot of owners specified)")]
        public bool ForwardToAllOwners { get; set; }

        [Comment(@"When a user DMs the bot with a message which is not a command
he will receive this message. Leave empty for no response.")]
        public string DmHelpText { get; set; }

        [Comment(@"This is the response for the .h command")]
        public string HelpText { get; set; }
        [Comment(@"List of modules and commands completely blocked on the bot")]
        public BlockedConfig Blocked { get; set; }

        [Comment(@"Which string will be used to recognize the commands")]
        public string Prefix { get; set; }
        [Comment(@"Whether the prefix will be a suffix, or prefix.
For example, if your prefix is ! you will run a command called 'cash' by typing either
'!cash @Someone' if your prefixIsSuffix: false or
'cash @Someone!' if your prefixIsSuffix: true")]
        public bool PrefixIsSuffix { get; set; }
        [Comment(@"Whether the bot will rotate through all specified statuses. This setting can be changed via .rots command. See RotatingStatuses submodule in Administration.")]
        public bool RotateStatuses { get; set; }

        [Comment(@"Amount of currency user will receive for every CENT pledged on patreon.")]
        public float PatreonCurrencyPerCent { get; set; }

        public string Prefixed(object text) => PrefixIsSuffix
            ? text.ToString() + Prefix
            : Prefix + text.ToString();
    }

    public struct BlockedConfig : IInitializable
    {
        [Comment(@"")]
        public List<string> Commands { get; set; }
        [Comment(@"")]
        public List<string> Modules { get; set; }

        public void Initialize()
        {
            Modules = new List<string>()
            {
                "nsfw"
            };
            Commands = new List<string>();
        }
    }

    public enum UpdateCheckType
    {
        Release, Commit, None
    }

    public struct ColorConfig : IInitializable
    {
        [Comment(@"")]
        public string Ok { get; set; }
        [Comment(@"")]
        public string Error { get; set; }
        [Comment(@"")]
        public string Pending { get; set; }

        public void Initialize()
        {
            Ok = "43B581";
            Error = "F04747";
            Pending = "FAA61A";
        }
    }
    public enum ConsoleOutputType
    {
        Normal = 0,
        Simple = 1,
        None = 2,
    }
}
