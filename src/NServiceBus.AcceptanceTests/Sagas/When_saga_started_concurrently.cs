namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_started_concurrently : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_start_single_saga()
    {
        var context = await Scenario.Define<Context>(c => { c.SomeId = Guid.NewGuid().ToString(); })
            .WithEndpoint<ConcurrentHandlerEndpoint>(b =>
            {
                b.When((session, ctx) =>
                {
                    var t1 = session.SendLocal(new StartMessageOne
                    {
                        SomeId = ctx.SomeId
                    });
                    var t2 = session.SendLocal(new StartMessageTwo
                    {
                        SomeId = ctx.SomeId
                    });
                    return Task.WhenAll(t1, t2);
                });
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.PlacedSagaId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(context.BilledSagaId, Is.Not.EqualTo(Guid.Empty));
        }
        Assert.That(context.BilledSagaId, Is.EqualTo(context.PlacedSagaId), "Both messages should have been handled by the same saga, but SagaIds don't match.");
    }

    public class Context : ScenarioContext
    {
        public string SomeId { get; set; }
        public Guid PlacedSagaId { get; set; }
        public Guid BilledSagaId { get; set; }
        public bool SagaCompleted { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(PlacedSagaId != Guid.Empty, BilledSagaId != Guid.Empty);
    }

    public class ConcurrentHandlerEndpoint : EndpointConfigurationBuilder
    {
        public ConcurrentHandlerEndpoint() =>
            EndpointSetup<DefaultServer>(b =>
            {
                b.LimitMessageProcessingConcurrencyTo(2);
                b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(3));
            });

        [Saga]
        public class ConcurrentlyStartedSaga(Context testContext) : Saga<ConcurrentlyStartedSagaData>,
            IAmStartedByMessages<StartMessageTwo>,
            IAmStartedByMessages<StartMessageOne>
        {
            public async Task Handle(StartMessageOne message, IMessageHandlerContext context)
            {
                Data.Placed = true;
                await context.SendLocal(new SuccessfulProcessing
                {
                    SagaId = Data.Id,
                    Type = nameof(StartMessageOne)
                });
                CheckForCompletion();
            }

            public async Task Handle(StartMessageTwo message, IMessageHandlerContext context)
            {
                Data.Billed = true;
                await context.SendLocal(new SuccessfulProcessing
                {
                    SagaId = Data.Id,
                    Type = nameof(StartMessageTwo)
                });
                CheckForCompletion();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ConcurrentlyStartedSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartMessageOne>(msg => msg.SomeId)
                    .ToMessage<StartMessageTwo>(msg => msg.SomeId);

            void CheckForCompletion()
            {
                if (!Data.Billed || !Data.Placed)
                {
                    return;
                }
                MarkAsComplete();
                testContext.SagaCompleted = true;
            }
        }

        public class ConcurrentlyStartedSagaData : ContainSagaData
        {
            public virtual string OrderId { get; set; }
            public virtual bool Placed { get; set; }
            public virtual bool Billed { get; set; }
        }

        // Intercepts messages sent out by saga
        [Handler]
        public class LogSuccessfulHandler(Context testContext) : IHandleMessages<SuccessfulProcessing>
        {
            public Task Handle(SuccessfulProcessing message, IMessageHandlerContext context)
            {
                switch (message.Type)
                {
                    case nameof(StartMessageOne):
                        testContext.PlacedSagaId = message.SagaId;
                        break;
                    case nameof(StartMessageTwo):
                        testContext.BilledSagaId = message.SagaId;
                        break;
                    default:
                        throw new Exception("Unknown type");
                }

                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class StartMessageOne : ICommand
    {
        public string SomeId { get; set; }
    }

    public class StartMessageTwo : ICommand
    {
        public string SomeId { get; set; }
    }

    public class SuccessfulProcessing : ICommand
    {
        public string Type { get; set; }
        public Guid SagaId { get; set; }
    }
}