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
        public async Task Should_not_load_type_dynamically()
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
            Assert.AreEqual($"Could not determine the message type from the '{Headers.EnclosedMessageTypes}' header", exception.InnerException.Message);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(cfg =>
                 {
                     cfg.Pipeline.Register(typeof(PatchEnclosedMessageTypeHeader), "Patches the EnclosedMessageTypeHeader to contain a type that requires Type.GetType to be invoked.");
                     var serializerSettings = cfg.UseSerialization<XmlSerializer>();
                     serializerSettings.DisableDynamicTypeLoading();
                     serializerSettings.DisableMessageTypeInference(); // just throw when we can't find the message type
                 }).ExcludeType<PatchMessage>();
            }

            class PatchEnclosedMessageTypeHeader : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public PatchEnclosedMessageTypeHeader(Context testContext) => this.testContext = testContext;

                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    testContext.MessageReceived = true;

                    context.Message.Headers[Headers.EnclosedMessageTypes] = typeof(PatchMessage).AssemblyQualifiedName;

                    return next();
                }
            }
        }

        public class Message : IMessage
        {
        }

        public class PatchMessage : IMessage
        {
        }
    }
}