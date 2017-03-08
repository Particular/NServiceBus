// disable obsolete warnings. Test will be removed in next major version
#pragma warning disable CS0618
namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_using_assembly_level_message_mapping_for_pub_sub : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task The_mapping_should_not_cause_publishing_to_non_subscribers()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<OtherEndpoint>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.EndpointsStarted, async session =>
                    {
                        await session.Publish(new MyEvent());
                        await session.Send(new DoneCommand());
                    })
                )
                .Done(c => c.CommandReceived || c.EventReceived)
                .Run();

            Assert.IsFalse(context.EventReceived);
            Assert.IsTrue(context.CommandReceived);
        }

        public class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
            public bool CommandReceived { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>()
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        c.MessageEndpointMappings = new MessageEndpointMappingCollection();
                        c.MessageEndpointMappings.Add(new MessageEndpointMapping
                        {
                            Endpoint = Conventions.EndpointNamingConvention(typeof(OtherEndpoint)),
                            AssemblyName = typeof(Publisher).Assembly.GetName().Name
                        });
                    });
            }
        }

        public class OtherEndpoint : EndpointConfigurationBuilder
        {
            public OtherEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    // do not subscribe to the event since we don't want to receive it.
                    c.DisableFeature<AutoSubscribe>();
                });
            }

            public class EventHandler : IHandleMessages<MyEvent>
            {
                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class DoneHandler : IHandleMessages<DoneCommand>
            {
                public DoneHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DoneCommand message, IMessageHandlerContext context)
                {
                    testContext.CommandReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyEvent : IEvent
        {
        }

        public class DoneCommand : ICommand
        {
        }
    }
}
#pragma warning restore CS0618