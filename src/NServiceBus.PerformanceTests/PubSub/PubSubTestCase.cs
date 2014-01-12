using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Transports.Msmq;
using NServiceBus.Transports.Msmq.Config;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using RavenTestMessages;
using Runner;

public class PubSubTestCase : TestCase
{
    protected int NumberOfSubscribers
    {
        get
        {
            int value;

            if (!int.TryParse(GetParameterValue("numberofsubscribers"), out value))
            {
                return 10;
            }

            return value;
        }
    }

    protected string StorageType
    {
        get
        {
            var value = GetParameterValue("storage");

            if (string.IsNullOrEmpty(value))
            {
                return "raven";
            }
            return value.ToLower();
        }
    }


    public override void Run()
    {
        var endpointName = "PubSubPerformanceTest";
     
     


        TransportConfigOverride.MaximumConcurrencyLevel = NumberOfThreads;

        Feature.Disable<Audit>();
        var config = Configure.With()
            .DefineEndpointName(endpointName)
            .DefaultBuilder()
            .UseTransport<Msmq>()
            .InMemoryFaultManagement();

        switch (StorageType)
        {
            case "raven":
                config.RavenSubscriptionStorage();
                break;
            case "inmemory":
                config.InMemorySubscriptionStorage();
                break;
            case "msmq":
                config.MsmqSubscriptionStorage();
                break;
        }

        using (var bus = config.InMemorySubscriptionStorage()
            .UnicastBus()
            .CreateBus())
        {
            var subscriptionStorage = Configure.Instance.Builder.Build<ISubscriptionStorage>();
         
            var testEventMessage = new MessageType(typeof(RavenTestEvent));

     
            subscriptionStorage.Init();


            var creator = new MsmqQueueCreator
            {
                Settings = new MsmqSettings { UseTransactionalQueues = true }
            };


            for (var i = 0; i < NumberOfSubscribers; i++)
            {
                var subscriberAddress = Address.Parse(endpointName + ".Subscriber" + i);
                creator.CreateQueueIfNecessary(subscriberAddress, null);
                subscriptionStorage.Subscribe(subscriberAddress, new List<MessageType>
                {
                    testEventMessage
                });
            }



            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();

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
