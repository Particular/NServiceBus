namespace SiteB
{
    using System;
    using NServiceBus;

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
}
