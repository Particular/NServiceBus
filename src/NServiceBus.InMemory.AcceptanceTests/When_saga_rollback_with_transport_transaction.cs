namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_saga_rollback_with_transport_transaction : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_rollback_saga_changes_when_transport_transaction_is_rolled_back()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b
                .When((session, ctx) => session.SendLocal(new StartSagaMessage { CorrelationId = ctx.TestRunId }))
                .DoNotFailOnErrorMessages()
                .CustomConfig(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.SendsAtomicWithReceive;
                }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HandlerInvocationCount, Is.EqualTo(2), "Handler should be called twice");
            Assert.That(context.SagaValue, Is.EqualTo("second"), "Saga state should reflect the second invocation only");
        }
    }

    [Test]
    public async Task Should_work_with_outbox_enabled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b
                .When((session, ctx) => session.SendLocal(new StartSagaMessage { CorrelationId = ctx.TestRunId }))
                .DoNotFailOnErrorMessages()
                .CustomConfig(c =>
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    c.EnableOutbox();
                }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HandlerInvocationCount, Is.EqualTo(2), "Handler should be called twice");
            Assert.That(context.SagaValue, Is.EqualTo("second"), "Saga state should reflect the second invocation only");
        }
    }

    public class Context : ScenarioContext
    {
        public int HandlerInvocationCount;
        public string SagaValue { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>((config, r) =>
        {
            config.LimitMessageProcessingConcurrencyTo(1);
            config.Recoverability().Immediate(settings => settings.NumberOfRetries(1));
            config.Pipeline.Register(
                "ThrowAfterFirstInvocation",
                new ThrowAfterFirstInvocationBehavior((Context)r.ScenarioContext),
                "Throws after the first handler invocation to test rollback");
        });

        [Saga]
        public class TestSaga(Context testContext) : Saga<TestSaga.TestSagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<CompleteSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                if (message.CorrelationId != testContext.TestRunId)
                {
                    return Task.CompletedTask;
                }

                var count = Interlocked.Increment(ref testContext.HandlerInvocationCount);
                Data.Value = count == 1 ? "first" : "second";

                return context.SendLocal(new CompleteSagaMessage { CorrelationId = testContext.TestRunId });
            }

            public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
            {
                if (message.CorrelationId != testContext.TestRunId)
                {
                    return Task.CompletedTask;
                }

                testContext.SagaValue = Data.Value;
                MarkAsComplete();
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationId)
                    .ToMessage<StartSagaMessage>(m => m.CorrelationId)
                    .ToMessage<CompleteSagaMessage>(m => m.CorrelationId);

            public class TestSagaData : ContainSagaData
            {
                public Guid CorrelationId { get; set; }
                public string Value { get; set; }
            }
        }

        class ThrowAfterFirstInvocationBehavior(Context testContext) : Behavior<IIncomingLogicalMessageContext>
        {
            public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
            {
                await next();

                if (context.Message.MessageType == typeof(StartSagaMessage)
                    && Volatile.Read(ref testContext.HandlerInvocationCount) == 1)
                {
                    throw new SimulatedException();
                }
            }
        }
    }

    public class StartSagaMessage : IMessage
    {
        public Guid CorrelationId { get; set; }
    }

    public class CompleteSagaMessage : IMessage
    {
        public Guid CorrelationId { get; set; }
    }
}
