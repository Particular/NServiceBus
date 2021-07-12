namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;
    using Transport;

    [Ignore("Failing. Just demonstrating leaking body modifications")]
    public class When_updating_message_body_in_policy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_rollback_body_changes_when_moving_to_error_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomPolicy>(c => c
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new FailingMessage { SomeProperty = "💔" })))
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.ErrorMessageBody != null)
                .Run();

            Assert.AreEqual(context.ReceivedMessageBody, context.ErrorMessageBody);
        }

        class Context : ScenarioContext
        {
            public byte[] ErrorMessageBody { get; set; }
            public byte[] ReceivedMessageBody { get; set; }
        }

        class EndpointWithCustomPolicy : EndpointConfigurationBuilder
        {
            public EndpointWithCustomPolicy() => EndpointSetup<DefaultServer>(c =>
            {
                c.SendFailedMessagesTo<ErrorSpy>();
                c.Recoverability().CustomPolicy((config, context) =>
                {
                    //TODO hardcoded value to replace part of the property value bytes. Doesn't work across serializers.
                    context.Message.Body[220] = 241;
                    return RecoverabilityAction.MoveToError(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
                });
            });

            class FailedMessageHandler : IHandleMessages<FailingMessage>
            {
                Context testContext;

                public FailedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    testContext.ReceivedMessageBody = (byte[])context.Extensions.Get<IncomingMessage>().Body.Clone();
                    throw new SimulatedException();
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy() => EndpointSetup<DefaultServer>();

            class FailedMessageHandler : IHandleMessages<FailingMessage>
            {
                Context testContext;

                public FailedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    testContext.ErrorMessageBody = (byte[])context.Extensions.Get<IncomingMessage>().Body.Clone();
                    return Task.CompletedTask;
                }
            }
        }

        class FailingMessage : IMessage
        {
            public string SomeProperty { get; set; }
        }
    }
}