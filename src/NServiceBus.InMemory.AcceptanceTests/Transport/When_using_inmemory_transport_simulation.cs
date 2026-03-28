namespace NServiceBus.AcceptanceTests.Transport;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_using_inmemory_transport_simulation : NServiceBusAcceptanceTest
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

    [Test]
    public async Task Should_delay_handler_emitted_sends_until_simulated_time_advances()
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
            .WithEndpoint<HandlerEmittedSendEndpoint>(builder => builder.When(session => session.SendLocal(new StartHandlerEmittedSends())))
            .Done(context => context.HandlerEmittedSendCount >= 2)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HandlerEmittedSendCount, Is.EqualTo(2));
            Assert.That(result.SecondHandlerEmittedSendAt - result.FirstHandlerEmittedSendAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    [Test]
    public async Task Should_delay_handler_emitted_delayed_delivery_until_simulated_time_advances()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));

        var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            DelayedDelivery =
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
            .WithEndpoint<HandlerEmittedDelayedDeliveryEndpoint>(builder => builder.When(session => session.SendLocal(new StartHandlerEmittedDelayedDelivery())))
            .Done(context => context.HandlerEmittedDelayedDeliveryCount >= 2)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HandlerEmittedDelayedDeliveryCount, Is.EqualTo(2));
            Assert.That(result.SecondHandlerEmittedDelayedDeliveryAt - result.FirstHandlerEmittedDelayedDeliveryAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
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

        public int HandlerEmittedSendCount;

        public DateTimeOffset FirstHandlerEmittedSendAt { get; set; }

        public DateTimeOffset SecondHandlerEmittedSendAt { get; set; }

        public int HandlerEmittedDelayedDeliveryCount;

        public DateTimeOffset FirstHandlerEmittedDelayedDeliveryAt { get; set; }

        public DateTimeOffset SecondHandlerEmittedDelayedDeliveryAt { get; set; }
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

    public class HandlerEmittedSendEndpoint : EndpointConfigurationBuilder
    {
        public HandlerEmittedSendEndpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class StartHandler : IHandleMessages<StartHandlerEmittedSends>
        {
            public async Task Handle(StartHandlerEmittedSends message, IMessageHandlerContext context)
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();

                await context.Send(new HandlerEmittedSendMessage(), sendOptions);
                await context.Send(new HandlerEmittedSendMessage(), sendOptions);
            }
        }

        [Handler]
        public class HandlerEmittedSendMessageHandler(Context testContext, FakeTimeProvider simulatedTime) : IHandleMessages<HandlerEmittedSendMessage>
        {
            public Task Handle(HandlerEmittedSendMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.HandlerEmittedSendCount);
                if (count == 1)
                {
                    testContext.FirstHandlerEmittedSendAt = simulatedTime.GetUtcNow();
                    simulatedTime.Advance(TimeSpan.FromSeconds(5));
                }
                else if (count == 2)
                {
                    testContext.SecondHandlerEmittedSendAt = simulatedTime.GetUtcNow();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class HandlerEmittedDelayedDeliveryEndpoint : EndpointConfigurationBuilder
    {
        public HandlerEmittedDelayedDeliveryEndpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class StartHandler(FakeTimeProvider simulatedTime) : IHandleMessages<StartHandlerEmittedDelayedDelivery>
        {
            public async Task Handle(StartHandlerEmittedDelayedDelivery message, IMessageHandlerContext context)
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();
                sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(5));

                await context.Send(new HandlerEmittedDelayedDeliveryMessage(), sendOptions);
                await context.Send(new HandlerEmittedDelayedDeliveryMessage(), sendOptions);

                simulatedTime.Advance(TimeSpan.FromSeconds(5));
            }
        }

        [Handler]
        public class HandlerEmittedDelayedDeliveryMessageHandler(Context testContext, FakeTimeProvider simulatedTime) : IHandleMessages<HandlerEmittedDelayedDeliveryMessage>
        {
            public Task Handle(HandlerEmittedDelayedDeliveryMessage message, IMessageHandlerContext context)
            {
                var count = Interlocked.Increment(ref testContext.HandlerEmittedDelayedDeliveryCount);
                if (count == 1)
                {
                    testContext.FirstHandlerEmittedDelayedDeliveryAt = simulatedTime.GetUtcNow();
                    simulatedTime.Advance(TimeSpan.FromSeconds(5));
                }
                else if (count == 2)
                {
                    testContext.SecondHandlerEmittedDelayedDeliveryAt = simulatedTime.GetUtcNow();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class SendDelayedMessage : IMessage;

    public class ReceiveRejectedMessage : IMessage;

    public class StartHandlerEmittedSends : IMessage;

    public class HandlerEmittedSendMessage : IMessage;

    public class StartHandlerEmittedDelayedDelivery : IMessage;

    public class HandlerEmittedDelayedDeliveryMessage : IMessage;
}
