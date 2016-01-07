namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_using_legacy_routing_configuration_combined_with_message_driven_pub_sub : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Messages_should_not_get_duplicated()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, async bus =>
                    {
                        await bus.Publish(new MyEvent());
                        await bus.Send(new DoneCommand());
                    })
                    )
                .WithEndpoint<Subscriber>(b => b.When((bus, context) => bus.Subscribe<MyEvent>()))
                .Done(c => c.Done)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.True(c.HandlerInvoked == 1))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public int HandlerInvoked { get; set; }
            public bool Subscribed { get; set; }
            public bool Done { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    context.Subscribed = true;
                }))
                    .AddMapping<MyEvent>(typeof(Subscriber))
                    .AddMapping<DoneCommand>(typeof(Subscriber));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.LimitMessageProcessingConcurrencyTo(1);
                })
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.HandlerInvoked++;
                    return Task.FromResult(0);
                }
            }

            public class DoneHandler : IHandleMessages<DoneCommand>
            {
                public Context Context { get; set; }

                public Task Handle(DoneCommand message, IMessageHandlerContext context)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class DoneCommand : ICommand
        {
        }

        public class MyEvent : IEvent
        {
        }
    }
}