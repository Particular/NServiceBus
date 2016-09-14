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
            var exToSend = new Exception
            {
                HelpLink = "test"
            };

            //the xml serializer will only transfer get/set props so we use help link for this test. You would have to use the
            // json serializer for all the props to transfer properly

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e
                    .When(session => session.SendLocal(new MessageWithPropContainingInterfaces
                    {
                        Exception = exToSend
                    })))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual("test", context.ReceivedProperty.HelpLink);
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
                EndpointSetup<DefaultServer>();
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