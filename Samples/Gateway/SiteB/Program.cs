using System;

namespace SiteB
{
    using Headquarter.Messages;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Installation.Environments;

    class Program
    {
        static void Main(string[] args)
        {
            Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                .UnicastBus()
                .FileShareDataBus(".\\databus")
                .RunGateway()//this line configures the gateway.
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
}
