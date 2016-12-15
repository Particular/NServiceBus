namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_overriding_local_address : NServiceBusAcceptanceTest
    {
        public static string ReceiverEndpointName => Conventions.EndpointNamingConvention(typeof(Receiver));
        public static string ReceiverQueueName => "q_" + ReceiverEndpointName;

        static string mappingFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(When_overriding_local_address) + ".xml");

        [SetUp]
        public void SetupMappingFile()
        {
            File.WriteAllText(mappingFilePath,
$@"<endpoints>
    <endpoint name=""{ReceiverEndpointName}"">
        <instance queue=""{ReceiverQueueName}""/>
    </endpoint>
</endpoints>");
        }

        [TearDown]
        public void DeleteMappingFile()
        {
            File.Delete(mappingFilePath);
        }

        [Test]
        public async Task Should_use_provided_instance_mapping()
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
                    routing.RouteToEndpoint(typeof(Message), ReceiverEndpointName);
                    routing.InstanceMappingFile().FilePath(mappingFilePath);
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.OverrideLocalAddress(ReceiverQueueName));
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