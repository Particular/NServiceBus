namespace NServiceBus.AcceptanceTests.Serialization
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_interface_message_where_child_is_excluded : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_process_base_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e.When(session => session.Send<ISomeMessage>(m => { })))
                .WithEndpoint<Receiver>()
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.UseSerialization<JsonSerializer>(); //only reproduces on json
                        c.ConfigureTransport().Routing().RouteToEndpoint(typeof(ISomeMessage), ReceiverEndpoint);
                    }
                );
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.UseSerialization<JsonSerializer>(); //only reproduces on json
                    })
                    .ExcludeType<ISomeMessage>();
            }

            class MessageHandler : IHandleMessages<ISomeBaseMessage>
            {
                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ISomeBaseMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public interface ISomeMessage : ISomeBaseMessage
        {
        }

        public interface ISomeBaseMessage : IMessage
        {
        }
    }
}