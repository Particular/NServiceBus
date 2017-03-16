namespace NServiceBus.Transport.Msmq.AcceptanceTests.SubscriptionStorage
{
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Persistence.Legacy;

    public class When_subscription_exists_for_tx_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_subscription()
        {
            var queuePath = $".\\private$\\{StorageQueueName}";

            if (MessageQueue.Exists(queuePath))
            {
                MessageQueue.Delete(queuePath);
            }

            MessageQueue.Create(queuePath, true);

            using (var queue = new MessageQueue(queuePath))
            {
                queue.Send(new Message
                {
                    Label = Conventions.EndpointNamingConvention(typeof(Subscriber)),
                    Body = typeof(MyEvent).AssemblyQualifiedName
                }, MessageQueueTransactionType.Single);
            }

            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                            b.When(c => c.EndpointsStarted, (session, c) => session.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber>()
                .Done(c => c.GotTheEvent)
                .Run();

            Assert.IsTrue(ctx.GotTheEvent);

            using (var queue = new MessageQueue(queuePath))
            {
                CollectionAssert.IsNotEmpty(queue.GetAllMessages());
            }
        }

        static string StorageQueueName = "msmq.acpt.txexistingsubscriptions";

        public class Context : ScenarioContext
        {
            public bool GotTheEvent { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.DisableFeature<AutoSubscribe>();
                    b.UsePersistence<MsmqPersistence>().SubscriptionQueue(StorageQueueName);
                });
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(
                    c => c.DisableFeature<AutoSubscribe>(),
                    m => m.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
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