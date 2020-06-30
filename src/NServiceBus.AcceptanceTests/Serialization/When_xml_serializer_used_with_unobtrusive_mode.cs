namespace NServiceBus.AcceptanceTests.Serialization
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_xml_serializer_used_with_unobtrusive_mode : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deserialize_message()
        {
            var expectedData = 1;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(s => s.SendLocal(new MyMessage
                {
                    Data = expectedData
                })))
                .Done(c => c.WasCalled)
                .Run();

            Assert.AreEqual(expectedData, context.Data);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public int Data { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningMessagesAs(t => t == typeof(MyMessage));
                    c.UseSerialization<XmlSerializer>();
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    testContext.Data = message.Data;
                    testContext.WasCalled = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyMessage
        {
            public int Data { get; set; }
        }
    }
}