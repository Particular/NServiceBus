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
        static string OtherEndpointName => Conventions.EndpointNamingConvention(typeof(OtherEndpoint));

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
                            Endpoint = OtherEndpointName,
                            AssemblyName = typeof(Publisher).Assembly.GetName().Name
                        });
                    })
                    .AddMapping<DoneCommand>(typeof(OtherEndpoint));
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
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.FromResult(0);
                }
            }

            public class DoneHandler : IHandleMessages<DoneCommand>
            {
                Context testContext;

                public DoneHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DoneCommand message, IMessageHandlerContext context)
                {
                    testContext.CommandReceived = true;
                    return Task.FromResult(0);
                }
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