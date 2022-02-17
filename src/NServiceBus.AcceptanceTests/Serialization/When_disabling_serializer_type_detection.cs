namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageInterfaces;
    using NServiceBus.Pipeline;
    using NServiceBus.Serialization;
    using NUnit.Framework;
    using Settings;

    class When_disabling_serializer_type_detection : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_pass_messages_without_types_header()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new MessageWithoutTypeHeader())))
                .Done(c => c.IncomingMessageReceived)
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsFalse(context.HandlerInvoked);
            Assert.AreEqual(1, context.FailedMessages.Single().Value.Count);
            Exception exception = context.FailedMessages.Single().Value.Single().Exception;
            Assert.IsInstanceOf<MessageDeserializationException>(exception);
            StringAssert.Contains($"Could not determine the message type from the '{Headers.EnclosedMessageTypes}' header", exception.InnerException.Message);
        }

        class Context : ScenarioContext
        {
            public bool HandlerInvoked { get; set; }
            public bool IncomingMessageReceived { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.Register(typeof(TypeHeaderRemovingBehavior), "Removes the EnclosedMessageTypes header from incoming messages");
                    var serializerSettings = c.UseSerialization<CustomSerializer>();
                    serializerSettings.DisableMessageTypeInference();
                });

            public class MessageHandler : IHandleMessages<MessageWithoutTypeHeader>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(MessageWithoutTypeHeader message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }

            class TypeHeaderRemovingBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public TypeHeaderRemovingBehavior(Context testContext) => this.testContext = testContext;

                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    testContext.IncomingMessageReceived = true;

                    context.Message.Headers.Remove(Headers.EnclosedMessageTypes);

                    return next();
                }
            }
        }

        public class MessageWithoutTypeHeader : IMessage
        {
        }

        class CustomSerializer : SerializationDefinition, IMessageSerializer
        {
            public string ContentType { get; } = "CustomSerializer";

            public void Serialize(object message, Stream stream)
            {
                stream.WriteByte(42); // need to write some byte for message serialization to work
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                // simulating type detection behavior implemented by the serializer
                return new object[]
                {
                    new MessageWithoutTypeHeader()
                };
            }

            public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings) => mapper => this;
        }
    }
}
