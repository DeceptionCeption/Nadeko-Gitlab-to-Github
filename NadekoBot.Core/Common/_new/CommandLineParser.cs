//using CommandLine;
//using Discord.Commands;
//using Discord.WebSocket;
//using NadekoBot.Core.Common.TypeReaders;
//using System;
//using System.Threading.Tasks;

//namespace Ayu.Discord.Commands.Parsers.Other
//{
//    public class OptionsParser<T> : NadekoTypeReader<Options<T>> where T : CommandOptions
//    {
//        public OptionsParser(DiscordSocketClient client, CommandService cmd) : base(client, cmd)
//        {

//        }

//        public override Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input, IServiceProvider services)
//        {
//            using (var p = new Parser(x =>
//            {
//                x.HelpWriter = null;
//                x.IgnoreUnknownArguments = false;
//            }))
//            {
//                var res = p.ParseArguments<T>(input.Split(" "));
//                var options = res.MapResult(x => x, x => (T)Activator.CreateInstance(typeof(T)));

//                if (res.Tag == ParserResultType.Parsed)
//                    return Task.FromResult(TypeReaderResult.FromSuccess(new Options<T>(options)));
//                else
//                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Parse failed."));
//            }
//        }
//    }

//    public class Options<T> where T : CommandOptions
//    {
//        public Options(T obj)
//        {
//            Data = obj;
//        }

//        public T Data { get; }
//    }

//    public abstract class CommandOptions
//    {
//        public CommandOptions()
//        {
//            NormalizeOptions();
//        }

//        public abstract void NormalizeOptions();
//    }
//}
