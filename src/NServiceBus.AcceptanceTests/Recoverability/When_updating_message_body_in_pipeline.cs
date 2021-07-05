namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Transport;

    public class When_updating_message_body_in_pipeline : NServiceBusAcceptanceTest
    {
        const int PropertyValue = 42424242;

        [Test]
        public async Task Should_rollback_body_changes_for_error_recoverability()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<BodyModifyingEndpoint>(e => e
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new SimpleMessage { SomeValue = PropertyValue })))
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.ErrorMessageBody != null)
                .Run();

            Assert.AreEqual(41414141, context.ReceivedPropertyValue);

            Assert.AreEqual(2, context.ReceivedMessageBodies.Count);
            Assert.AreEqual(context.ReceivedMessageBodies[0], context.ReceivedMessageBodies[1], "because body modifications should not persist across retries");

            Assert.AreEqual(context.ReceivedMessageBodies[0], context.ErrorMessageBody);
            Assert.AreEqual(PropertyValue, context.ErrorPropertyValue);
        }

        class Context : ScenarioContext
        {
            public List<byte[]> ReceivedMessageBodies { get; set; } = new List<byte[]>();
            public byte[] ErrorMessageBody { get; set; }
            public int ReceivedPropertyValue { get; set; }
            public int ErrorPropertyValue { get; set; }
        }

        class BodyModifyingEndpoint : EndpointConfigurationBuilder
        {
            public BodyModifyingEndpoint() => EndpointSetup<DefaultServer>(c =>
            {
                c.SendFailedMessagesTo<ErrorSpy>();
                c.Recoverability().Immediate(s => s.NumberOfRetries(1));
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
                    throw new SimulatedException();
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
                    testContext.ReceivedMessageBodies.Add((byte[])context.Message.Body.Clone());
                    var bodyString = Encoding.UTF8.GetString(context.Message.Body);
                    bodyString = bodyString.Replace(PropertyValue.ToString(), PropertyValue.ToString().Replace('2', '1'));
                    context.UpdateMessage(Encoding.UTF8.GetBytes(bodyString));
                    return next();
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
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
                    testContext.ErrorMessageBody = context.Extensions.Get<IncomingMessage>().Body;
                    testContext.ErrorPropertyValue = message.SomeValue;
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