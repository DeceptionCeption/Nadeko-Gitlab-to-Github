using Grpc.Core;
using Nadeko.Common;
using Nadeko.Microservices;
using SearchImagesService.Common.Db;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace SearchImagesService
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            SerilogConfig.Setup("SIMG");

            StartService(new CredsService());

            await Task.Delay(-1);
        }

        public static int StartService(CredsService creds)
        {
            var db = new SearchImagesDb(creds);
            Server server = new Server
            {
                Services = { SearchImages.BindService(new SearchImagesService(db)) },
                Ports = { new ServerPort("0.0.0.0", 2452, ServerCredentials.Insecure) }
            };
            server.Start();

            var boundPort = server.Ports.First().BoundPort;

            Log.Logger.Information("SearchImages service started on port: {Port}", boundPort);

            return boundPort;
        }

    }
}

