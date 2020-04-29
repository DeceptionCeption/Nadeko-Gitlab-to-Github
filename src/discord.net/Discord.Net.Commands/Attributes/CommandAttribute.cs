using System;

namespace Discord.Commands
{
    /// <summary>
    ///     Marks the execution information for a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        ///     Gets the text that has been set to be recognized as a command.
        /// </summary>
        public string Text { get; }
        public string[] Aliases { get; }
        /// <summary>
        ///     Specifies the <see cref="RunMode" /> of the command. This affects how the command is executed.
        /// </summary>
        public RunMode RunMode { get; set; } = RunMode.Default;
        public bool? IgnoreExtraArgs { get; }

        /// <inheritdoc />
        public CommandAttribute()
        {
            Text = null;
        }

        /// <summary>
        ///     Initializes a new <see cref="CommandAttribute" /> attribute with the specified name.
        /// </summary>
        /// <param name="text">The name of the command.</param>
        public CommandAttribute(string text)
        {
            Text = text;
        }
        public CommandAttribute(string name, string[] aliases = null)
        {
            Text = name;
            Aliases = aliases ?? Array.Empty<string>();
            IgnoreExtraArgs = false;
        }
    }
}
