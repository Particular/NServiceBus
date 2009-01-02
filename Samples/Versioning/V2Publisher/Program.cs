using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Logging;
using NServiceBus;

namespace V2Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            LogManager.GetLogger("hello").Debug("Started.");

            var bus = NServiceBus.Configure.With()
                .SpringBuilder()
                .MsmqSubscriptionStorage()
                .XmlSerializer("http://www.Publisher.com")
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                .CreateBus()
                .Start();

            Console.WriteLine("Press 'Enter' to publish a message. To exit, press 'q' and then 'Enter'.");

            string read;
            while ((read = Console.ReadLine().ToLower()) != "q")
            {
                bus.Publish<V2.Messages.SomethingHappened>(sh => { sh.SomeData = 1; sh.MoreInfo = "It's a secret."; });

                Console.WriteLine("Published event.");
            }
        }
    }
}
