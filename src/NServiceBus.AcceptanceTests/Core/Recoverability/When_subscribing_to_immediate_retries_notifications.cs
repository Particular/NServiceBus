namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Faults;
    using NUnit.Framework;

    public class When_subscribing_to_immediate_retries_notifications : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_trigger_notification_on_immediate_retry()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryingEndpoint>(b =>
                {
                    b.DoNotFailOnErrorMessages();
                    b.When((session, c) => session.SendLocal(new MessageToBeRetried()));
                })
                .Done(c => c.MessageSentToError)
                .Run();

            Assert.IsInstanceOf<SimulatedException>(context.LastImmediateRetryInfo.Exception);
            // Immediate Retries max retries = 3 means we will be processing 4 times. Delayed Retries max retries = 2 means we will do 3 * Immediate Retries
            Assert.AreEqual(4, context.TotalNumberOfHandlerInvocations);
            Assert.AreEqual(3, context.TotalNumberOfImmediateRetriesEventInvocations);
            Assert.AreEqual(2, context.LastImmediateRetryInfo.RetryAttempt);
        }

        class Context : ScenarioContext
        {
            public int TotalNumberOfImmediateRetriesEventInvocations { get; set; }
            public int TotalNumberOfHandlerInvocations { get; set; }
            public bool MessageSentToError { get; set; }
            public ImmediateRetryMessage LastImmediateRetryInfo { get; set; }
        }

        public class RetryingEndpoint : EndpointConfigurationBuilder
        {
            public RetryingEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var testContext = (Context)context.ScenarioContext;

                    var recoverability = config.Recoverability();
                    recoverability.Failed(f => f.OnMessageSentToErrorQueue(failedMessage =>
                    {
                        testContext.MessageSentToError = true;
                        return Task.FromResult(0);
                    }));

                    recoverability.Immediate(immediateRetriesSettings =>
                    {
                        immediateRetriesSettings.NumberOfRetries(3);
                        immediateRetriesSettings.OnMessageBeingRetried(retryInfo =>
                        {
                            testContext.TotalNumberOfImmediateRetriesEventInvocations++;
                            testContext.LastImmediateRetryInfo = retryInfo;
                            return Task.FromResult(0);
                        });
                    });
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    Context.TotalNumberOfHandlerInvocations++;

                    throw new SimulatedException("Simulated exception message");
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}