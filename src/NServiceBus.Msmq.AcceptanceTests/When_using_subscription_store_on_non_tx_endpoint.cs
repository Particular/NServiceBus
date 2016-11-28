namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Config;
    using Config.ConfigurationSource;
    using Features;
    using NUnit.Framework;
    using Persistence.Legacy;

    public class When_using_subscription_store_on_non_tx_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_persist_subscriptions()
        {
            var queuePath = $".\\private$\\{StorageQueueName}";

            if (MessageQueue.Exists(queuePath))
            {
                MessageQueue.Delete(queuePath);
            }

            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                            b.When(c => c.Subscribed, (session, c) => session.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber>(b => b.When(session => session.Subscribe<MyEvent>()))
                .Done(c => c.GotTheEvent)
                .Run(TimeSpan.FromSeconds(10));

            Assert.IsTrue(ctx.GotTheEvent);

            using (var queue = new MessageQueue(queuePath))
            {
                CollectionAssert.IsNotEmpty(queue.GetAllMessages());
            }
        }

        static string StorageQueueName = "msmq.acpt.nontxsubscriptions";

        public class Context : ScenarioContext
        {
            public bool GotTheEvent { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
                    b.DisableFeature<AutoSubscribe>();
                    b.UsePersistence<MsmqPersistence>();
                    b.UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.None)
                        .ConnectionString("useTransactionalQueues=false");
                });
            }

            class QueueNameOverride : IProvideConfiguration<MsmqSubscriptionStorageConfig>
            {
                public MsmqSubscriptionStorageConfig GetConfiguration()
                {
                    return new MsmqSubscriptionStorageConfig
                    {
                        Queue = StorageQueueName
                    };
                }
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.DisableFeature<AutoSubscribe>();
                        c.UseTransport<MsmqTransport>()
                            .Transactions(TransportTransactionMode.None)
                            .ConnectionString("useTransactionalQueues=false");
                    })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    TestContext.GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}