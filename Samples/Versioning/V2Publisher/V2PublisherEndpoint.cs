using System;
using NServiceBus;
using NServiceBus.Host;

namespace V2Publisher
{
    public class V2PublisherEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IBus Bus { get; set; }
        
        public void OnStart()
        {
            Console.WriteLine("Press 'Enter' to publish a message, Ctrl + C to exit.");

            while (Console.ReadLine() != null)
            {
                Bus.Publish<V2.Messages.SomethingHappened>(sh => { sh.SomeData = 1; sh.MoreInfo = "It's a secret."; });

                Console.WriteLine("Published event.");
            }
        }

        public void OnStop()
        {
        }

        public Configure ConfigureBus(Configure config)
        {
            return config
                .SpringBuilder()
                .MsmqSubscriptionStorage()
                .XmlSerializer("http://www.Publisher.com")
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);
        }
    }
}