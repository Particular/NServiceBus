namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Transport;

    public class When_updating_message_body : NServiceBusAcceptanceTest
    {
        const int PropertyValue = 42424242;

        [Test]
        public async Task Should_rollback_body_changes_for_audits()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<BodyModifyingEndpoint>(e => e
                    .When(s => s.SendLocal(new SimpleMessage { SomeValue = PropertyValue })))
                .WithEndpoint<AuditSpy>()
                .Done(c => c.AuditedMessageBody != null)
                .Run();

            Assert.AreEqual(context.ReceivedMessageBody, context.AuditedMessageBody);
            Assert.AreEqual(41414141, context.ReceivedPropertyValue);
            Assert.AreEqual(PropertyValue, context.AuditedPropertyValue);
        }

        class Context : ScenarioContext
        {
            public byte[] ReceivedMessageBody { get; set; }
            public byte[] AuditedMessageBody { get; set; }
            public int ReceivedPropertyValue { get; set; }
            public int AuditedPropertyValue { get; set; }
        }

        class BodyModifyingEndpoint : EndpointConfigurationBuilder
        {
            public BodyModifyingEndpoint() => EndpointSetup<DefaultServer>(c =>
            {
                c.AuditProcessedMessagesTo<AuditSpy>();
                c.Pipeline.Register(typeof(BodyModifyingBehavior), "Modifies the message body");
            });

            class SimpleMessageHandler : IHandleMessages<SimpleMessage>
            {
                Context testContext;

                public SimpleMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SimpleMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedPropertyValue = message.SomeValue;
                    return Task.CompletedTask;
                }
            }

            class BodyModifyingBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                Context testContext;

                public BodyModifyingBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    testContext.ReceivedMessageBody = (byte[])context.Message.Body.Clone();
                    var bodyString = Encoding.UTF8.GetString(context.Message.Body);
                    bodyString = bodyString.Replace(PropertyValue.ToString(), PropertyValue.ToString().Replace('2', '1'));
                    context.UpdateMessage(Encoding.UTF8.GetBytes(bodyString));
                    return next();
                }
            }
        }

        class AuditSpy : EndpointConfigurationBuilder
        {
            public AuditSpy()
            {
                EndpointSetup<DefaultServer>();
            }

            class SimpleMessageHandler : IHandleMessages<SimpleMessage>
            {
                Context testContext;

                public SimpleMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SimpleMessage message, IMessageHandlerContext context)
                {
                    testContext.AuditedMessageBody = context.Extensions.Get<IncomingMessage>().Body;
                    testContext.AuditedPropertyValue = message.SomeValue;
                    return Task.CompletedTask;
                }
            }
        }

        class SimpleMessage : IMessage
        {
            public int SomeValue { get; set; }
        }
    }
}