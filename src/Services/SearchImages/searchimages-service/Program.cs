using Grpc.Core;
using Nadeko.Microservices;
using Serilog;
using System;
using System.Threading.Tasks;

namespace SearchImagesService
{
    public class Program
    {
        private const int port =
            2452
            ;
        public static async Task Main(string[] args)
        {
            SetupLogger();

            Server server = new Server
            {
                Services = { SearchImages.BindService(new SearchImagesService()) },
                Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
            };
            server.Start();

            Log.Logger.Information("SearchImages service started on port: {Port}", port);

            await Task.Delay(-1);
        }

        private static void SetupLogger()
        {
            var log = new LoggerConfiguration()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                .WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
                {
                    FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                    MinimumLogEventLevel = Serilog.Events.LogEventLevel.Debug,
                })
                .CreateLogger();

            Log.Logger = log;
        }
    }
}

