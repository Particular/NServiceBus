namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using NServiceBus.Routing.Legacy;
    using NUnit.Framework;

    public class When_subscribing_from_a_worker : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Event_should_be_delivered_to_the_distributor()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.Subscribed, (session, c) => session.Publish(new MyEvent())))
                .WithEndpoint<Distributor>()
                .WithEndpoint<Worker>()
                .Done(c => c.DeliveredToDistributor || c.DeliveredToWorker)
                .Run();

            Assert.IsTrue(context.DeliveredToDistributor);
            Assert.IsFalse(context.DeliveredToWorker);
        }

        public class Context : ScenarioContext
        {
            public bool Subscribed { get; set; }
            public bool DeliveredToDistributor { get; set; }
            public bool DeliveredToWorker { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.DisableFeature<AutoSubscribe>();
                    b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
                });
            }
        }

        static string DistributorEndpoint => Conventions.EndpointNamingConvention(typeof(Distributor));

        public class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>());
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.DeliveredToDistributor = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(
                    c =>
                    {
                        c.EnlistWithLegacyMSMQDistributor(DistributorEndpoint, DistributorEndpoint, 1);
                        var routing = c.UseTransport<MsmqTransport>().Routing();
                        routing.RegisterPublisher(typeof(MyEvent), Conventions.EndpointNamingConvention(typeof(Publisher)));
                    });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.DeliveredToWorker = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}