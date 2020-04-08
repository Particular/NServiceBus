namespace NServiceBus.GenericHost
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class ProgramFullControl
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .UseNServiceBus(hostContext =>
                {
                    var configuration = new EndpointConfiguration("Test");
                    configuration.Conventions().DefiningCommandsAs(_ => throw new Exception("Boom!"));

                    configuration.UseTransport<LearningTransport>();
                    configuration.UsePersistence<LearningPersistence>();

                    return configuration;
                })
                .Build();

            using (host)
            {
                Console.WriteLine("Starting!");

                await host.StartAsync();

                Console.WriteLine("Started! Press <enter> to stop.");

                Console.ReadLine();

                Console.WriteLine("Stopping!");

                await host.StopAsync();

                Console.WriteLine("Stopped!");
            }
        }
    }
}
