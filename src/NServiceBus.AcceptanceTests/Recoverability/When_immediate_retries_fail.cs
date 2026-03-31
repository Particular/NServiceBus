namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;

public class When_immediate_retries_fail : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_do_delayed_retries()
    {
        Requires.DelayedDelivery();

        var exception = Assert.ThrowsAsync<MessageFailedException>(async () => await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<DelayedRetryEndpoint>(b => b
                .When((session, ctx) => session.SendLocal(new MessageToBeRetried
                {
                    Id = ctx.Id
                })))
            .Run());

        var context = (Context)exception.ScenarioContext;
        Assert.That(context.NumberOfRetriesAttempted, Is.GreaterThanOrEqualTo(1), "Should retry one or more times");
    }

    static TimeSpan Delay = TimeSpan.FromMilliseconds(1);

    public class Context : ScenarioContext
    {
        public Guid Id { get; set; }

        public int NumberOfTimesInvoked { get; set; }

        public int NumberOfRetriesAttempted => NumberOfTimesInvoked - 1 < 0 ? 0 : NumberOfTimesInvoked - 1;
    }

    public class DelayedRetryEndpoint : EndpointConfigurationBuilder
    {
        public DelayedRetryEndpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.Recoverability().Immediate(i => i.NumberOfRetries(0));
                config.Recoverability()
                    .Delayed(settings =>
                    {
                        settings.NumberOfRetries(1);
                        settings.TimeIncrease(Delay);
                    });
            });

        [Handler]
        public class MessageToBeRetriedHandler(Context testContext) : IHandleMessages<MessageToBeRetried>
        {
            public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
            {
                if (message.Id != testContext.Id)
                {
                    return Task.CompletedTask; // messages from previous test runs must be ignored
                }

                testContext.NumberOfTimesInvoked++;

                throw new SimulatedException();
            }
        }
    }

    public class MessageToBeRetried : IMessage
    {
        public Guid Id { get; set; }
    }
}