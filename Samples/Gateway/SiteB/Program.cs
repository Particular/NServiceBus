using System;

namespace SiteB
{
    using Headquarter.Messages;
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
                .AllowDiscovery()
                .GatewayWithInMemoryPersistence()
                .CreateBus()
                .Start();
;


           
            Console.WriteLine("Waiting for price updates from the headquarter - press any key to exit");

            Console.ReadLine();
        }
    }

    internal class RunInstallers : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            Configure.Instance.ForInstallationOn<Windows>().Install();
        }
    }


    public class PriceUpdatedMessageHandler : IHandleMessages<PriceUpdated>
    {
        public void Handle(PriceUpdated message)
        {
            Console.WriteLine("Price update received");
        }
    }
}
