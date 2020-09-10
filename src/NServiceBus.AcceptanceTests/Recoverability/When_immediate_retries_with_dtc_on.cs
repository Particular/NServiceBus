namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_immediate_retries_with_dtc_on : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_do_the_configured_number_of_retries()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((session, c) => session.SendLocal(new MessageToBeRetried
                    {
                        Id = c.Id
                    }))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.GaveUpOnRetries)
                .Run();

            Assert.AreEqual(maxretries + 1, context.NumberOfTimesInvoked, $"The Immediate Retries should retry {maxretries} times");
            Assert.AreEqual(maxretries, context.Logs.Count(l => l.Message
                .StartsWith($"Immediate Retry is going to retry message '{context.PhysicalMessageId}' because of an exception:")));
        }

        const int maxretries = 4;

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool GaveUpOnRetries { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((b, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    b.Recoverability().Failed(f => f.OnMessageSentToErrorQueue(message =>
                    {
                        scenarioContext.GaveUpOnRetries = true;
                        return Task.FromResult(0);
                    }));
                    var recoverability = b.Recoverability();
                    recoverability.Immediate(settings => settings.NumberOfRetries(maxretries));
                    recoverability.Delayed(settings => settings.NumberOfRetries(0));
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
                    if (message.Id != testContext.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    testContext.PhysicalMessageId = context.MessageId;
                    testContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }

                Context testContext;
            }
        }


        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}
