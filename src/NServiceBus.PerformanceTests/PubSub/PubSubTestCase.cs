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
using RavenTestMessages;
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
            return "raven";
        }
        return value.ToLower();
    }


    public override void Run()
    {
        TransportConfigOverride.MaximumConcurrencyLevel = NumberOfThreads;

        Feature.Disable<Audit>();

        Configure.Transactions.Enable();

        var config = Configure.With()
            .DefineEndpointName("PubSubPerformanceTest")
            .DefaultBuilder()
            .UseTransport<Msmq>()
            .InMemoryFaultManagement();

        switch (GetStorageType())
        {
            case "raven":
                config.RavenSubscriptionStorage();
                break;
            case "inmemory":
                config.UsePersistence<InMemory>();
                break;
            case "msmq":
                config.MsmqSubscriptionStorage();
                break;
        }

        using (var bus = config.UnicastBus()
            .CreateBus())
        {


            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();

            var subscriptionStorage = Configure.Instance.Builder.Build<ISubscriptionStorage>();
         
            var testEventMessage = new MessageType(typeof(RavenTestEvent));

     
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
          x => bus.SendLocal(new PerformRavenPublish()));


            Statistics.StartTime = DateTime.Now;
            bus.Start();

            while (Interlocked.Read(ref Statistics.NumberOfMessages) < NumberMessages)
                Thread.Sleep(1000);


            Statistics.Dump();
        }

    }
}

class PublishRavenEventHandler : IHandleMessages<PerformRavenPublish>
{
    public IBus Bus { get; set; }
    public void Handle(PerformRavenPublish message)
    {
        Bus.Publish<RavenTestEvent>();
    }
}

namespace RavenTestMessages
{
    public class PerformRavenPublish : IMessage
    {
    }

    public class RavenTestEvent : IEvent
    {
    }
}
