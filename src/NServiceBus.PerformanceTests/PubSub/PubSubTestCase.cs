using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Persistence.Legacy;
using NServiceBus.Settings;
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

        var configuration = new BusConfiguration();

        configuration.EndpointName("PubSubPerformanceTest");
        configuration.RijndaelEncryptionService("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
        configuration.EnableInstallers();
        configuration.UseTransport<MsmqTransport>();
        configuration.DisableFeature<Audit>();
        configuration.EnableFeature<PrimeSubscriptionStorage>();

        configuration.GetSettings().Set("NumberOfSubscribers", GetNumberOfSubscribers());

        switch (GetStorageType())
        {
            case "inmemory":
                configuration.UsePersistence<InMemoryPersistence>();
                break;
            case "msmq":
                configuration.UsePersistence<MsmqPersistence>();
                break;
        }


        using (var bus = Bus.Create(configuration))
        {
            var sendContext = bus.CreateSendContext();
            Parallel.For(
                0,
                NumberMessages,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = NumberOfThreads
                },
                x => sendContext.SendLocalAsync(new PerformPublish()).GetAwaiter().GetResult());


            Statistics.StartTime = DateTime.Now;
            bus.StartAsync().GetAwaiter().GetResult();

            while (Interlocked.Read(ref Statistics.NumberOfMessages) < NumberMessages)
            {
                Thread.Sleep(1000);
            }


            Statistics.Dump();
        }
    }
}

public class PrimeSubscriptionStorage : Feature
{
    public PrimeSubscriptionStorage()
    {
        RegisterStartupTask<PrimeSubscriptionStorageTask>();
    }

    protected internal override void Setup(FeatureConfigurationContext context)
    {
    }

    class PrimeSubscriptionStorageTask : FeatureStartupTask
    {
        public ReadOnlySettings Settings { get; set; }

        public IInitializableSubscriptionStorage SubscriptionStorage { get; set; }

        protected override void OnStart()
        {
            PrimeSubscriptionStorage(SubscriptionStorage);
        }

        void PrimeSubscriptionStorage(IInitializableSubscriptionStorage subscriptionStorage)
        {
            var testEventMessage = new MessageType(typeof(TestEvent));

            subscriptionStorage.Init();

            var creator = new QueueCreator(new MsmqSettings
            {
                UseTransactionalQueues = true
            });

            var numberOfSubscribers = Settings.Get<int>("NumberOfSubscribers");
            for (var i = 0; i < numberOfSubscribers; i++)
            {
                var subscriberAddress = "PubSubPerformanceTest.Subscriber" + i;
                creator.CreateQueueIfNecessary(subscriberAddress, WindowsIdentity.GetCurrent().Name);

                using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    subscriptionStorage.Subscribe(subscriberAddress, new List<MessageType>
                    {
                        testEventMessage
                    }, new ContextBag()).GetAwaiter().GetResult();

                    tx.Complete();
                }
            }
        }
    }
}

class PublishEventHandler : IHandleMessages<PerformPublish>
{
    public Task Handle(PerformPublish message, IMessageHandlerContext context)
    {
        return context.PublishAsync<TestEvent>();
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
