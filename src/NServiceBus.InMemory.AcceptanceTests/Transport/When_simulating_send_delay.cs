namespace NServiceBus.AcceptanceTests.Transport;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

public class When_simulating_send_delay : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_delay_sends_until_simulated_time_advances()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));

        var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Send =
            {
                RateLimit = new InMemoryRateLimitOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(5)
                }
            }
        });

        await using var _ = broker;
        var result = await Scenario.Define<Context>()
            .WithServices(services =>
            {
                services.AddSingleton(broker);
                services.AddSingleton(simulatedTime);
            })
            .WithEndpoint<SendDelayEndpoint>(builder => builder.When(async session =>
            {
                await session.SendLocal(new SendDelayedMessage());
                await session.SendLocal(new SendDelayedMessage());
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SendDelayedCount, Is.EqualTo(2));
            Assert.That(result.SecondSendDelayedAt - result.FirstSendDelayedAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    public class Context : ScenarioContext
    {
        public int SendDelayedCount;

        public DateTimeOffset FirstSendDelayedAt { get; set; }

        public DateTimeOffset SecondSendDelayedAt { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(SendDelayedCount >= 2);
    }

    public class SendDelayEndpoint : EndpointConfigurationBuilder
    {
        public SendDelayEndpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class Handler(Context testContext, FakeTimeProvider simulatedTime) : IHandleMessages<SendDelayedMessage>
        {
            public Task Handle(SendDelayedMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.SendDelayedCount);
                if (count == 1)
                {
                    testContext.FirstSendDelayedAt = simulatedTime.GetUtcNow();
                    simulatedTime.Advance(TimeSpan.FromSeconds(5));
                }
                else if (count == 2)
                {
                    testContext.SecondSendDelayedAt = simulatedTime.GetUtcNow();
                }

                testContext.MaybeCompleted();

                return Task.CompletedTask;
            }
        }
    }

    public class SendDelayedMessage : IMessage;
}