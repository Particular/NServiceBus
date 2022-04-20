namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_skipping_serialization_with_nested_send : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_skip_serialization_for_nested_send()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e
                    .When(s => s.Send(new MessageWithoutSerialization { SomeProperty = "Some property value" })))
                .WithEndpoint<Receiver>()
                .Done(c => c.NestedMessageReceived)
                .Run(TimeSpan.FromSeconds(15));

            Assert.IsTrue(context.NestedMessageReceived, "the serialization should the nested message should not be skipped");
            Assert.AreEqual("Some property value for NestedMessage", context.NestedMessagePropertyValue, "the message sould be correctly serialized");
            Assert.IsFalse(context.MessageWithSkippedSerializationReceived, "NServiceBus should discard messages without a body");
        }

        class Context : ScenarioContext
        {
            public bool MessageWithSkippedSerializationReceived { get; set; }
            public bool NestedMessageReceived { get; set; }
            public string NestedMessagePropertyValue { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ConfigureTransport().Routing().RouteToEndpoint(typeof(NestedMessage).Assembly, Conventions.EndpointNamingConvention(typeof(Receiver)));
                    c.Pipeline.Register(new SkipSerializationBehavior(), $"Skips serialization for {nameof(MessageWithoutSerialization)}");
                    c.Pipeline.Register(new NestedSendBehavior(), $"Sends a {nameof(NestedMessage)} from the outgoing pipeline");
                });
            }

            class SkipSerializationBehavior : Behavior<IOutgoingLogicalMessageContext>
            {
                public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    if (context.Message.MessageType == typeof(MessageWithoutSerialization))
                    {
                        context.SkipSerialization();
                    }

                    return next();
                }
            }

            class NestedSendBehavior : Behavior<IOutgoingPhysicalMessageContext>
            {
                public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
                {
                    var logicalMessage = context.Extensions.Get<OutgoingLogicalMessage>();
                    if (logicalMessage.MessageType != typeof(NestedMessage))
                    {
                        await context.Send(new NestedMessage { SomeProperty = "Some property value for NestedMessage" });
                    }

                    await next();
                }
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver() => EndpointSetup<DefaultServer>();

            class MessageHandler : IHandleMessages<MessageWithoutSerialization>, IHandleMessages<NestedMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithoutSerialization message, IMessageHandlerContext context)
                {
                    testContext.MessageWithSkippedSerializationReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(NestedMessage message, IMessageHandlerContext context)
                {
                    testContext.NestedMessageReceived = true;
                    testContext.NestedMessagePropertyValue = message.SomeProperty;
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageWithoutSerialization : IMessage
        {
            public string SomeProperty { get; set; }
        }

        public class NestedMessage : IMessage
        {
            public string SomeProperty { get; set; }
        }
    }
}