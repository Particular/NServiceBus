namespace NServiceBus.AcceptanceTests.Transport;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

public class When_simulating_handler_emitted_delayed_delivery : NServiceBusAcceptanceTest
{
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
            .WithEndpoint<Endpoint>(builder => builder.When(session => session.SendLocal(new StartMessage())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HandlerEmittedDelayedDeliveryCount, Is.EqualTo(2));
            Assert.That(result.SecondHandlerEmittedDelayedDeliveryAt - result.FirstHandlerEmittedDelayedDeliveryAt, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }
    }

    public class Context : ScenarioContext
    {
        public int HandlerEmittedDelayedDeliveryCount;

        public DateTimeOffset FirstHandlerEmittedDelayedDeliveryAt { get; set; }

        public DateTimeOffset SecondHandlerEmittedDelayedDeliveryAt { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(HandlerEmittedDelayedDeliveryCount >= 2);
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((configure, _) => configure.LimitMessageProcessingConcurrencyTo(1));

        [Handler]
        public class StartHandler(FakeTimeProvider simulatedTime) : IHandleMessages<StartMessage>
        {
            public async Task Handle(StartMessage message, IMessageHandlerContext context)
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
        public class DelayedHandler(Context testContext, FakeTimeProvider simulatedTime) : IHandleMessages<HandlerEmittedDelayedDeliveryMessage>
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

                testContext.MaybeCompleted();

                return Task.CompletedTask;
            }
        }
    }

    public class StartMessage : IMessage;

    public class HandlerEmittedDelayedDeliveryMessage : IMessage;
}
