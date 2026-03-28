namespace NServiceBus.AcceptanceTests.Transport;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

public class When_simulating_queue_override_precedence : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_honor_queue_override_precedence_through_endpoint_surface()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Send =
            {
                RateLimit = new InMemoryRateLimitOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(30)
                }
            }
        };
        options.ForQueue(QueueOverrideEndpoint.EndpointName).Send.RateLimit = new InMemoryRateLimitOptions
        {
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(30)
        };

        var broker = new InMemoryBroker(options);

        await using var _ = broker;
        var result = await Scenario.Define<Context>()
            .WithServices(services =>
            {
                services.AddSingleton(broker);
                services.AddSingleton(simulatedTime);
            })
            .WithEndpoint<QueueOverrideEndpoint>(builder => builder.When(async session =>
            {
                await session.SendLocal(new QueueOverrideMessage());
                await session.SendLocal(new QueueOverrideMessage());
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.QueueOverrideDeliveredCount, Is.EqualTo(2));
            Assert.That(result.SecondQueueOverrideDeliveredAt - result.FirstQueueOverrideDeliveredAt, Is.EqualTo(TimeSpan.Zero));
        }
    }

    public class Context : ScenarioContext
    {
        public int QueueOverrideDeliveredCount;

        public DateTimeOffset FirstQueueOverrideDeliveredAt { get; set; }

        public DateTimeOffset SecondQueueOverrideDeliveredAt { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(QueueOverrideDeliveredCount >= 2);
    }

    public class QueueOverrideEndpoint : EndpointConfigurationBuilder
    {
        public const string EndpointName = "queue-override-endpoint";

        public QueueOverrideEndpoint()
        {
            EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));
            CustomEndpointName(EndpointName);
        }

        [Handler]
        public class Handler(Context testContext, FakeTimeProvider simulatedTime) : IHandleMessages<QueueOverrideMessage>
        {
            public Task Handle(QueueOverrideMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.QueueOverrideDeliveredCount);
                if (count == 1)
                {
                    testContext.FirstQueueOverrideDeliveredAt = simulatedTime.GetUtcNow();
                }
                else if (count == 2)
                {
                    testContext.SecondQueueOverrideDeliveredAt = simulatedTime.GetUtcNow();
                }

                testContext.MaybeCompleted();

                return Task.CompletedTask;
            }
        }
    }

    public class QueueOverrideMessage : IMessage;
}