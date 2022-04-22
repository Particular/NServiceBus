namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_custom_policy_moves_to_overridden_error_queue : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_to_defined_error_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(b => b
                    .DoNotFailOnErrorMessages()
                    .When((session, ctx) => session.SendLocal(new InitiatingMessage
                    {
                        Id = ctx.TestRunId
                    }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.IsTrue(context.MessageMovedToErrorQueue);
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.Recoverability().CustomPolicy((c, ec) =>
                        RecoverabilityAction.MoveToError(Conventions.EndpointNamingConvention(typeof(ErrorSpy))));

                    config.SendFailedMessagesTo("error");
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        class ErrorSpy : EndpointFromTemplate<DefaultServer>
        {
            class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public InitiatingMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (initiatingMessage.Id == testContext.TestRunId)
                    {
                        testContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}