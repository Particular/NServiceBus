using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Transports.Msmq;
using NServiceBus.Transports.Msmq.Config;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using PublishTestMessages;
using Runner;
using MsmqTransport = NServiceBus.MsmqTransport;

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

        var configuration = new BusConfiguration();

        configuration.EndpointName("PubSubPerformanceTest");
        configuration.EnableInstallers();
        configuration.UseTransport<MsmqTransport>();
        configuration.DisableFeature<Audit>();

        switch (GetStorageType())
        {
            case "inmemory":
                configuration.UsePersistence<InMemoryPersistence>();
                break;
            case "msmq":
                configuration.UsePersistence<NServiceBus.Persistence.Legacy.MsmqPersistence>();
                break;
        }

        
        using (var bus = Bus.Create(configuration))
        {
            var subscriptionStorage = ((NServiceBus.Unicast.UnicastBus) bus).Builder.Build<ISubscriptionStorage>();


            PrimeSubscriptionStorage(subscriptionStorage);

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

    void PrimeSubscriptionStorage(ISubscriptionStorage subscriptionStorage)
    {
        var testEventMessage = new MessageType(typeof(TestEvent));


        subscriptionStorage.Init();


        var creator = new MsmqQueueCreator
        {
            Settings = new MsmqSettings
            {
                UseTransactionalQueues = true
            }
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
