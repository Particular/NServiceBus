using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Transports.Msmq;
using NServiceBus.Transports.Msmq.Config;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using PublishTestMessages;
using Runner;

public class PubSubTestCase : TestCase
{
    int GetNumberOfSubscribers()
    {
        int value;
        if (!int.TryParse(GetParameterValue("numberofsubscribers"), out value))
        {
            return 10;
        }
        return value;
    }

    string GetStorageType()
    {
        var value = GetParameterValue("storage");

        if (string.IsNullOrEmpty(value))
        {
            return "inmemory";
        }
        return value.ToLower();
    }


    public override void Run()
    {
        TransportConfigOverride.MaximumConcurrencyLevel = NumberOfThreads;

        var config = Configure.With(o => o.EndpointName("PubSubPerformanceTest"))
            .DefaultBuilder()
            .UseTransport<Msmq>()
            .InMemoryFaultManagement()
            .Features(f=>f.Disable<Audit>());

        switch (GetStorageType())
        {
            case "inmemory":
                config.UsePersistence<InMemory>();
                break;
            case "msmq":
                config.MsmqSubscriptionStorage();
                break;
        }

        config.EnableInstallers();
        using (var bus = config.CreateBus())
        {
            var subscriptionStorage = config.Builder.Build<ISubscriptionStorage>();

            var testEventMessage = new MessageType(typeof(TestEvent));


            subscriptionStorage.Init();


            var creator = new MsmqQueueCreator
            {
                Settings = new MsmqSettings { UseTransactionalQueues = true }
            };

            for (var i = 0; i < GetNumberOfSubscribers(); i++)
            {
                var subscriberAddress = Address.Parse("PubSubPerformanceTest.Subscriber" + i);
                creator.CreateQueueIfNecessary(subscriberAddress, null);

                using (var tx = new TransactionScope())
                {
                    subscriptionStorage.Subscribe(subscriberAddress, new List<MessageType>
                        {
                            testEventMessage
                        });

                    tx.Complete();
                }
            }

            Parallel.For(
          0,
          NumberMessages,
          new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads },
          x => bus.SendLocal(new PerformPublish()));


            Statistics.StartTime = DateTime.Now;
            bus.Start();

            while (Interlocked.Read(ref Statistics.NumberOfMessages) < NumberMessages)
                Thread.Sleep(1000);


            Statistics.Dump();
        }

    }
}

class PublishEventHandler : IHandleMessages<PerformPublish>
{
    public IBus Bus { get; set; }
    public void Handle(PerformPublish message)
    {
        Bus.Publish<TestEvent>();
    }
}

namespace PublishTestMessages
{
    public class PerformPublish : IMessage
    {
    }

    public class TestEvent : IEvent
    {
    }
}
