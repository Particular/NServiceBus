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

    class When_disabling_serializer_type_inference : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_deserialize_messages_without_types_header()
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

        [Test]
        public async Task Should_not_deserialize_messages_with_unknown_type_header()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new UnknownMessage())))
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
                    c.Pipeline.Register(typeof(TypeHeaderManipulationBehavior), "Removes the EnclosedMessageTypes header from incoming messages");
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
                    return Task.CompletedTask;
                }
            }

            class TypeHeaderManipulationBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public TypeHeaderManipulationBehavior(Context testContext) => this.testContext = testContext;

                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    testContext.IncomingMessageReceived = true;

                    if (context.MessageHeaders[Headers.EnclosedMessageTypes].Contains(typeof(MessageWithoutTypeHeader).FullName))
                    {
                        context.Message.Headers.Remove(Headers.EnclosedMessageTypes);
                    }
                    else if (context.MessageHeaders[Headers.EnclosedMessageTypes].Contains(typeof(UnknownMessage).FullName))
                    {
                        context.Message.Headers[Headers.EnclosedMessageTypes] = "SomeNamespace.SomeMessageType";
                    }

                    return next();
                }
            }
        }

        public class MessageWithoutTypeHeader : IMessage
        {
        }

        public class UnknownMessage : IMessage
        {
        }

        class CustomSerializer : SerializationDefinition, IMessageSerializer
        {
            public string ContentType { get; } = "CustomSerializer";

            public void Serialize(object message, Stream stream)
            {
                stream.WriteByte(42); // need to write some byte for message serialization to work
            }

            public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null)
            {
                if (messageTypes?.Count > 0)
                {
                    throw new InvalidOperationException("Did not expect message types to be detected in this test");
                }

                throw new InvalidOperationException("Should not invoke deserializer without type information");
            }


            public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings) => _ => this;
        }
    }
}
