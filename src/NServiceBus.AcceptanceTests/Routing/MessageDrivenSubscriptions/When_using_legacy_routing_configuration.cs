namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_using_legacy_routing_configuration : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Events_routes_and_command_routes_should_be_kept_separate()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, async session =>
                    {
                        await session.Publish(new MyEvent());
                        await session.Send(new DoneCommand());
                    })
                )
                .WithEndpoint<Subscriber>(b => b.When(session => session.Subscribe<MyEvent>()))
                .Done(c => c.Done)
                .Run();

            Assert.True(context.HandlerInvoked == 1);
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
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; });
                    b.ConfigureTransport().Routing().RouteToEndpoint(typeof(DoneCommand), typeof(Subscriber));
                });
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
                    },
                    metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
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