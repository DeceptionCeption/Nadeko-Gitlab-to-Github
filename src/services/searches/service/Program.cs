using Grpc.Core;
using Nadeko.Common;
using Nadeko.Microservices;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace SearchesService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            SerilogConfig.Setup("SRCH");

            StartService(new CredsService());

            await Task.Delay(-1);
        }

        public static int StartService(CredsService creds)
        {
            Server server = new Server
            {
                Services = { Searches.BindService(new SearchesService(creds)) },
                Ports = { new ServerPort(ServiceConsts.Host, ServiceConsts.Port, ServerCredentials.Insecure) }
            };
            server.Start();

            var boundPort = server.Ports.First().BoundPort;

            Log.Logger.Information("Searches service started on port: {Port}", boundPort);

            return boundPort;
        }
    }
}
