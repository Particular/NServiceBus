namespace NServiceBus.AcceptanceTests.Transport;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_using_inmemory_transport_simulation : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_delay_sends_until_fake_time_advances()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));

        var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime,
            Send =
            {
                RateLimit = new InMemoryRateLimitOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(5)
                }
            }
        });

        await using var _ = broker ;
        var result = await Scenario.Define<Context>()
            .WithServices(services =>
            {
                services.AddSingleton(broker);
                services.AddSingleton(fakeTime);
            })
            .WithEndpoint<SendDelayedEndpoint>(builder => builder.When(async session =>
            {
                await session.SendLocal(new SendDelayedMessage());
                await session.SendLocal(new SendDelayedMessage());
            }))
            .Done(context => context.SendDelayedCount >= 2)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.SendDelayedCount, Is.EqualTo(2));
            Assert.That(result.SecondSendDelayedAt - result.FirstSendDelayedAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    [Test]
    public async Task Should_retry_rejected_receives_after_fake_time_advances()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));

        var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime,
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

        await using var _ = broker ;
        var result = await Scenario.Define<Context>()
            .WithServices(services =>
            {
                services.AddSingleton(broker);
                services.AddSingleton(fakeTime);
            })
            .WithEndpoint<RejectReceiveEndpoint>(builder => builder.When(async session =>
            {
                await session.SendLocal(new ReceiveRejectedMessage());
                await session.SendLocal(new ReceiveRejectedMessage());
            }))
            .Done(context => context.ReceiveRejectedCount >= 2)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ReceiveRejectedCount, Is.EqualTo(2));
            Assert.That(result.SecondReceiveRejectedAt - result.FirstReceiveRejectedAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    public class Context : ScenarioContext
    {
        public int SendDelayedCount;

        public int ReceiveRejectedCount;

        public DateTimeOffset FirstSendDelayedAt { get; set; }

        public DateTimeOffset SecondSendDelayedAt { get; set; }

        public DateTimeOffset FirstReceiveRejectedAt { get; set; }

        public DateTimeOffset SecondReceiveRejectedAt { get; set; }
    }

    public class SendDelayedEndpoint : EndpointConfigurationBuilder
    {
        public SendDelayedEndpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class SendDelayedMessageHandler(Context testContext, FakeTimeProvider fakeTimeProvider) : IHandleMessages<SendDelayedMessage>
        {
            public Task Handle(SendDelayedMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.SendDelayedCount);
                if (count == 1)
                {
                    testContext.FirstSendDelayedAt = fakeTimeProvider.GetUtcNow();
                    fakeTimeProvider.Advance(TimeSpan.FromSeconds(5));
                }
                else if (count == 2)
                {
                    testContext.SecondSendDelayedAt = fakeTimeProvider.GetUtcNow();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class RejectReceiveEndpoint : EndpointConfigurationBuilder
    {
        public RejectReceiveEndpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class ReceiveRejectedMessageHandler(Context testContext, FakeTimeProvider fakeTimeProvider) : IHandleMessages<ReceiveRejectedMessage>
        {
            public Task Handle(ReceiveRejectedMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.ReceiveRejectedCount);
                if (count == 1)
                {
                    testContext.FirstReceiveRejectedAt = fakeTimeProvider.GetUtcNow();
                    fakeTimeProvider.Advance(TimeSpan.FromSeconds(5));
                }
                else if (count == 2)
                {
                    testContext.SecondReceiveRejectedAt = fakeTimeProvider.GetUtcNow();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class SendDelayedMessage : IMessage;

    public class ReceiveRejectedMessage : IMessage;
}