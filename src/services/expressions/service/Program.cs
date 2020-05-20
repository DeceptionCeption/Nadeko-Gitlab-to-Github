using ExpressionsService.Database;
using Grpc.Core;
using Nadeko.Common;
using Nadeko.Microservices;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace ExpressionsService
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            SerilogConfig.Setup("EXPR");

            StartService(new CredsService());

            await Task.Delay(-1);
        }

        public static int StartService(CredsService creds)
        {
            var exprdb = new ExpressionsDb(creds);

            Server server = new Server
            {
                Services = { Expressions.BindService(new ExpressionsService(exprdb)) },
                Ports = { new ServerPort("0.0.0.0", 2452, ServerCredentials.Insecure) }
            };
            server.Start();

            var boundPort = server.Ports.First().BoundPort;

            Log.Logger.Information("Expressions service started on port: {Port}", boundPort);

            return boundPort;
        }
    }
}

