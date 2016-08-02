namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    public class When_overriding_address_translation : NServiceBusAcceptanceTest
    {
        public static string DefaultReceiverAddress => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_use_the_overridden_rule()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e.When(c => c.Send(new Message())))
                .WithEndpoint<Receiver>()
                .Done(c => c.ReceivedMessage)
                .Run();

            Assert.That(context.ReceivedMessage, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool ReceivedMessage { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var transport = c.UseTransport<MsmqTransport>();
                    var routing = transport.Routing();

                    // only configure logical endpoint in routing
                    routing.RouteToEndpoint(typeof(Message), DefaultReceiverAddress);

                    // add translation rule
                    transport.OverrideAddressTranslation(a => "q_" + a.EndpointInstance.Endpoint);
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.OverrideInputQueueName("q_" + DefaultReceiverAddress));
            }

            public class MessageHandler : IHandleMessages<Message>
            {
                Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.ReceivedMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Message : ICommand
        {
        }
    }
}