namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_concrete_message_with_interface_props : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_involve_mapper()
        {
            var exToSend = new Exception("boom")
            {
                HelpLink = "test"
            };

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e
                    .When(session => session.SendLocal(new MessageWithPropContainingInterfaces
                    {
                        Exception = exToSend
                    })))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual("test", context.ReceivedProperty.HelpLink);
            Assert.AreEqual("boom", context.ReceivedProperty.Message);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public Exception ReceivedProperty { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                //note: we need to use json.net since our xml serializer can't handle properties with readonly properties properly
                EndpointSetup<DefaultServer>(c => c.UseSerialization<JsonSerializer>());
            }

            class MessageWithPropContainingInterfacesHandler : IHandleMessages<MessageWithPropContainingInterfaces>
            {
                public MessageWithPropContainingInterfacesHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(MessageWithPropContainingInterfaces messageWithPropContainingInterfaces, IMessageHandlerContext context)
                {
                    scenarioContext.ReceivedProperty = messageWithPropContainingInterfaces.Exception;
                    scenarioContext.MessageReceived = true;
                    return Task.FromResult(0);
                }

                Context scenarioContext;
            }
        }

        class MessageWithPropContainingInterfaces : IMessage
        {
            public Exception Exception { get; set; }
        }
    }
}