namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_immediate_retries_are_enabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_do_the_configured_number_of_retries()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<RetryEndpoint>(b => b
                .When((session, c) => session.SendLocal(new MessageToBeRetried()))
                .DoNotFailOnErrorMessages())
            .Done(c => c.ForwardedToErrorQueue)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ForwardedToErrorQueue, Is.True);
            Assert.That(context.NumberOfTimesInvoked, Is.EqualTo(numberOfRetries + 1), "Message should be retried 5 times immediately");
            Assert.That(context.Logs.Count(l => l.Message
                .StartsWith($"Immediate Retry is going to retry message '{context.MessageId}' because of an exception:")), Is.EqualTo(numberOfRetries));
        }
    }

    const int numberOfRetries = 5;

    class Context : ScenarioContext
    {
        public int NumberOfTimesInvoked { get; set; }

        public bool ForwardedToErrorQueue { get; set; }

        public string MessageId { get; set; }
    }

    public class RetryEndpoint : EndpointConfigurationBuilder
    {
        public RetryEndpoint()
        {
            EndpointSetup<DefaultServer>((config, context) =>
            {
                var scenarioContext = (Context)context.ScenarioContext;
                config.Recoverability().Failed(f => f.OnMessageSentToErrorQueue((message, _) =>
                {
                    scenarioContext.ForwardedToErrorQueue = true;
                    return Task.CompletedTask;
                }));

                var recoverability = config.Recoverability();
                recoverability.Immediate(immediate => immediate.NumberOfRetries(numberOfRetries));
            });
        }

        class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
        {
            public MessageToBeRetriedHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                testContext.MessageId = context.MessageId;
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