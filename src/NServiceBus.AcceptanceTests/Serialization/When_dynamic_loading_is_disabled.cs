namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_dynamic_loading_is_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_wrap_xml_content()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(session => session.SendLocal(new Message()))
                )
                .Done(c => c.MessageReceived)
                .Run();

            Assert.AreEqual(1, context.FailedMessages.Single().Value.Count);
            Exception exception = context.FailedMessages.Single().Value.Single().Exception;
            Assert.IsInstanceOf<MessageDeserializationException>(exception);
            Assert.AreEqual("Could not determine type for node: 'Message'.", exception.InnerException.Message);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer, Context>((cfg, context) =>
                 {
                     cfg.Pipeline.Register(typeof(PatchEnclosedMessageTypeHeader), "Patches the EnclosedMessageTypeHeader to contain a type that requires Type.GetType to be invoked.");
                     cfg.DisableDynamicTypeLoading();
                 });
            }

            class PatchEnclosedMessageTypeHeader : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public PatchEnclosedMessageTypeHeader(Context testContext) => this.testContext = testContext;

                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    testContext.MessageReceived = true;

                    context.Message.Headers[Headers.EnclosedMessageTypes] = typeof(PatchMessage).FullName;

                    return next();
                }
            }
        }

        class Message : IMessage
        {
        }

        class PatchMessage
        {
        }
    }
}