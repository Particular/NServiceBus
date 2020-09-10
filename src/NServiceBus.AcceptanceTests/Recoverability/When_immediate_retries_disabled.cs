namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_immediate_retries_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_retry_the_message_using_immediate_retries()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b =>
                {
                    b.DoNotFailOnErrorMessages();
                    b.When((session, c) => session.SendLocal(new MessageToBeRetried
                    {
                        ContextId = c.Id
                    }));
                })
                .Done(c => c.GaveUp)
                .Run();

            Assert.AreEqual(1, context.NumberOfTimesInvoked, "No Immediate Retry should be in use if NumberOfRetries is set to 0");
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int NumberOfTimesInvoked { get; set; }

            public bool GaveUp { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    configure.Recoverability().Immediate(immediate => immediate.NumberOfRetries(0));
                    configure.Recoverability().Failed(f => f.OnMessageSentToErrorQueue(message =>
                    {
                        scenarioContext.GaveUp = true;
                        return Task.FromResult(0);
                    }));
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public MessageToBeRetriedHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if (testContext.Id != message.ContextId)
                    {
                        return Task.FromResult(0);
                    }
                    testContext.NumberOfTimesInvoked++;
                    throw new SimulatedException();
                }

                Context testContext;
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid ContextId { get; set; }
        }
    }
}