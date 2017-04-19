namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Settings;

    public class When_subscribing_with_scaled_out_subscriber : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_round_robin()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(async s =>
                {
                    await s.Publish(new MyEvent());
                    await s.Publish(new MyEvent());
                }))
                .WithEndpoint<Subscriber>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<Subscriber>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .Done(c => c.HandledOnDistriminator.Count >= 2)
                .Run();

            Assert.That(context.HandledOnDistriminator, Does.Contain("1"));
            Assert.That(context.HandledOnDistriminator, Does.Contain("2"));
            Assert.That(context.HandledOnDistriminator.Count, Is.EqualTo(2));
        }

        class Context : ScenarioContext
        {
            public List<string> HandledOnDistriminator { get; } = new List<string>();
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => { }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            class MyHandler : IHandleMessages<MyEvent>
            {
                Context testContext;
                ReadOnlySettings settings;

                public MyHandler(Context context, ReadOnlySettings settings)
                {
                    this.settings = settings;
                    testContext = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    var discriminator = settings.Get<string>("EndpointInstanceDiscriminator");
                    testContext.HandledOnDistriminator.Add(discriminator);
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}