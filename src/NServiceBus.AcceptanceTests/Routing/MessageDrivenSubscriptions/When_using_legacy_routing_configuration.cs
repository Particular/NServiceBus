namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_using_legacy_routing_configuration : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Events_routes_and_command_routes_should_be_kept_separate()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, async session =>
                    {
                        await session.Publish(new MyEvent());
                        await session.Send(new DoneCommand());
                    })
                )
                .WithEndpoint<Subscriber>(b => b.When((session, context) => session.Subscribe<MyEvent>()))
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
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) => { context.Subscribed = true; }))
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