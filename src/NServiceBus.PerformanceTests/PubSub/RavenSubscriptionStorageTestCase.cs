using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Persistence.Raven;
using NServiceBus.Persistence.Raven.SubscriptionStorage;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using Raven.Client.Document;

public class RavenSubscriptionStorageTestCase : TestCase
{
    int NumberOfSubscribers
    {
        get
        {
            return int.Parse(parameters["numberofsubscribers"]);
        }
    }


    public override void Run()
    {
        using (var store = new DocumentStore())
        {
            store.Url = "http://localhost:8080";
            store.Initialize();

            var ravenStorage = new RavenSubscriptionStorage(new StoreAccessor(store));

            ravenStorage.Init();

            var subscriptionStorage = (ISubscriptionStorage) ravenStorage;
            var testEventMessage = new MessageType(typeof(TestEvent));

            for (var i = 0; i < NumberOfSubscribers; i++)
            {
                subscriptionStorage.Subscribe(Address.Parse("endpoint" + i), new List<MessageType>
                {
                    testEventMessage
                });
            }

            var sw = new Stopwatch();
         
          
            sw.Start();
            Parallel.For(
                0,
                NumberMessages,
                new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads },
                x =>
                {
                    using (var tx = new TransactionScope())
                    {
                        subscriptionStorage.GetSubscriberAddressesForMessage(new[] { new MessageType(typeof(TestEvent)) });
    
                        tx.Complete();
                    }
                });
            sw.Stop();

            var elapsedMS = sw.ElapsedMilliseconds;

            Console.Out.WriteLine("Average (ms): {0} - Total:{1}", NumberMessages / elapsedMS, elapsedMS);

        }
    }
}

public class TestEvent
{
}