using System;
using NServiceBus.Gateway.Persistence.Sql;

namespace SiteB
{
    using Headquarter.Messages;
    using NServiceBus.Gateway.Persistence;
    using NServiceBus.Unicast;
    using log4net.Appender;
    using log4net.Core;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Installation.Environments;

    class Program
    {
        static void Main(string[] args)
        {
            Configure.With()
                .Log4Net<ColoredConsoleAppender>(a => { a.Threshold = Level.Warn; })
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                .UnicastBus()
                .FileShareDataBus(".\\databus")
                .RunGateway()//this line configures the gateway.
                .RunTimeoutManagerWithInMemoryPersistence()
                .UseInMemoryGatewayPersister() //this tells nservicebus to use Raven to store messages ids for deduplication. If omitted RavenDB will be used by default
                //.RunGateway(typeof(SqlPersistence)) // Uncomment this to use Gateway SQL persister (please see InitializeGatewayPersisterConnectionString.cs in this sample).
                .CreateBus()
                .Start();

            Console.WriteLine("Waiting for price updates from the headquarter - press any key to exit");

            Console.ReadLine();
        }
    }

    internal class RunInstallers : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            //run the installers to  make sure that all queues are created
            Configure.Instance.ForInstallationOn<Windows>().Install();
        }
    }


    public class PriceUpdatedMessageHandler : IHandleMessages<PriceUpdated>
    {
        public void Handle(PriceUpdated message)
        {
            Console.WriteLine("Price update received");
            Console.WriteLine("DataBusProperty: " + message.SomeLargeString.Value);
        }
    }

    public class DeduplicationCleanup : IWantToRunWhenTheBusStarts
    {
        public InMemoryPersistence MemoryPersistence { get; set; }
        public void Run()
        {
            Schedule.Every(TimeSpan.FromSeconds(10))
                //delete all ID's older than 1 minute
                .Action(() =>
                    {
                        var numberOfDeletedMessages =
                            MemoryPersistence.DeleteDeliveredMessages(DateTime.UtcNow.AddMinutes(1));

                        Console.Out.WriteLine("InMemory store cleared, number of items deleted: {0}", numberOfDeletedMessages);
                    });
        }
    }
}
