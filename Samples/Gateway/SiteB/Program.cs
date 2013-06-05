using System;

namespace SiteB
{
    using Headquarter.Messages;
    using NServiceBus.Gateway.Persistence;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Installation.Environments;

    class Program
    {
        static void Main()
        {
            Configure.With()
                .DefaultBuilder()
                .UseTransport<Msmq>()
                .UnicastBus()
                .FileShareDataBus(".\\databus")
                .UseInMemoryTimeoutPersister()
                .RunGateway() //this line configures the gateway

                    // This tells NServiceBus to use memory to persist & deduplicate messages arriving from NServiceBus v3.X.
                    // If omitted, RavenDB will be used by default. Required for backwards compatibility
                    .UseInMemoryGatewayPersister()

                    // This tells NServiceBus to use memory to deduplicate message ids arriving from NServiceBus v4.X.
                    // If omitted, RavenDB will be used by default
                    .UseInMemoryGatewayDeduplication()

                // Uncomment lines below to use NHibernate persister & deduplication for gateway messages
                // (create a new database called gateway in \SQLEXPRESS - see App.config for connection strings and other settings)
                //    .UseNHibernateGatewayPersister()
                //    .UseNHibernateGatewayDeduplication()

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

    public class DeduplicationCleanup : IWantToRunWhenBusStartsAndStops
    {
        public InMemoryPersistence MemoryPersistence { get; set; }
        public void Start()
        {
            Schedule.Every(TimeSpan.FromMinutes(1))
                //delete all ID's older than 5 minutes
                .Action(() =>
                    {
                        var numberOfDeletedMessages =
                            MemoryPersistence.DeleteDeliveredMessages(DateTime.UtcNow.AddMinutes(-5));

                        Console.Out.WriteLine("InMemory store cleared, number of items deleted: {0}", numberOfDeletedMessages);
                    });
        }

        public void Stop()
        {
        }
    }
}
