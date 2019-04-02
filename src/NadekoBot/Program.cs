using NadekoBot.Core.Services;
using Serilog;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NadekoBot
{
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            SetupLogger();
            if (args.Length == 2
                && int.TryParse(args[0], out int shardId)
                && int.TryParse(args[1], out int parentProcessId))
            {
                await new NadekoBot(shardId, parentProcessId)
                    .RunAndBlockAsync();
            }
            else
            {
                await new ShardsCoordinator()
                    .RunAsync()
                    .ConfigureAwait(false);
#if DEBUG
                await new NadekoBot(0, Process.GetCurrentProcess().Id)
                    .RunAndBlockAsync();
#else
                await Task.Delay(-1);
#endif
            }
        }

        private static void SetupLogger()
        {
            var log = new LoggerConfiguration()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                .CreateLogger();

            Log.Logger = log;
        }
    }
}
