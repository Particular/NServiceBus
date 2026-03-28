namespace NServiceBus.AcceptanceTests.Transport;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

public class When_simulating_receive_reject : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_retry_rejected_receives_after_simulated_time_advances()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));

        var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Receive =
            {
                Mode = InMemorySimulationMode.Reject,
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
            .WithEndpoint<ReceiveRejectEndpoint>(builder => builder.When(async session =>
            {
                await session.SendLocal(new ReceiveRejectedMessage());
                await session.SendLocal(new ReceiveRejectedMessage());
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ReceiveRejectedCount, Is.EqualTo(2));
            Assert.That(result.SecondReceiveRejectedAt - result.FirstReceiveRejectedAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    public class Context : ScenarioContext
    {
        public int ReceiveRejectedCount;

        public DateTimeOffset FirstReceiveRejectedAt { get; set; }

        public DateTimeOffset SecondReceiveRejectedAt { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(ReceiveRejectedCount >= 2);
    }

    public class ReceiveRejectEndpoint : EndpointConfigurationBuilder
    {
        public ReceiveRejectEndpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class Handler(Context testContext, FakeTimeProvider simulatedTime) : IHandleMessages<ReceiveRejectedMessage>
        {
            public Task Handle(ReceiveRejectedMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.ReceiveRejectedCount);
                if (count == 1)
                {
                    testContext.FirstReceiveRejectedAt = simulatedTime.GetUtcNow();
                    simulatedTime.Advance(TimeSpan.FromSeconds(5));
                }
                else if (count == 2)
                {
                    testContext.SecondReceiveRejectedAt = simulatedTime.GetUtcNow();
                }

                testContext.MaybeCompleted();

                return Task.CompletedTask;
            }
        }
    }

    public class ReceiveRejectedMessage : IMessage;
}