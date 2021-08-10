namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class Sub_to_multiple_pubs : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_subscribe_to_all_registered_publishers_of_same_type()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>(e => e
                    .When(s => s.Subscribe<SomeEvent>()))
                .WithEndpoint<Publisher>(e => e
                    .CustomConfig(cfg =>
                    {
                        cfg.OverrideLocalAddress("Publisher1");
                        cfg.OnEndpointSubscribed<Context>((args, ctx) => ctx.SubscribedToPublisher1 = true);
                    }))
                .WithEndpoint<Publisher>(e => e
                    .CustomConfig(cfg =>
                    {
                        cfg.OverrideLocalAddress("Publisher2");
                        cfg.OnEndpointSubscribed<Context>((args, ctx) => ctx.SubscribedToPublisher2 = true);
                    }))
                .Done(c => c.SubscribedToPublisher1 && c.SubscribedToPublisher2)
                .Run();

            Assert.That(context.SubscribedToPublisher1, Is.True);
            Assert.That(context.SubscribedToPublisher2, Is.True);

        }

        class Context : ScenarioContext
        {
            public bool SubscribedToPublisher1 { get; set; }
            public bool SubscribedToPublisher2 { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                }, metadata =>
                {
                    metadata.RegisterPublisherFor<SomeEvent>("Publisher1");
                    metadata.RegisterPublisherFor<SomeEvent>("Publisher2");
                });
            }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class SomeEvent : IEvent
        {
        }
    }
}
