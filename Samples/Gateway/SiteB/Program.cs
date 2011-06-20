using System;

namespace SiteB
{
    using Headquarter.Messages;
    using log4net.Appender;
    using log4net.Core;
    using NServiceBus;

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
                .GatewayWithInMemoryPersistence()
                .CreateBus()
                .Start();

            Console.WriteLine("Waiting for price updates from the headquarter - press any key to exit");

            Console.ReadLine();
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
