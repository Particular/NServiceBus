namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_configuring_unrecoverable_exception : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_move_to_error_queue_without_retries()
        {
            Context context = null;

            var exception = Assert.ThrowsAsync<MessagesFailedException>(async () =>
            {
                await Scenario.Define<Context>(ctx => context = ctx)
                    .WithEndpoint<EndpointWithFailingHandler>(b => b
                        .When((session, ctx) => session.SendLocal(new InitiatingMessage
                        {
                            Id = ctx.TestRunId
                        }))
                    )
                    .Done(c => c.FailedMessages.Any())
                    .Run();
            });

            var failedMessage = exception.FailedMessages.Single();
            Assert.That(failedMessage.Exception, Is.TypeOf<CustomException>());
            Assert.AreEqual(1, context.HandlerInvoked);
        }

        class Context : ScenarioContext
        {
            public int HandlerInvoked { get; set; }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.Recoverability().AddUnrecoverableException(typeof(CustomException));
                    config.Recoverability().Immediate(i => i.NumberOfRetries(2));
                    config.Recoverability().Delayed(d => d.NumberOfRetries(2));
                });
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    TestContext.HandlerInvoked++;
                    throw new CustomException();
                }
            }
        }

        class CustomException : SimulatedException
        {
        }

        public class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}