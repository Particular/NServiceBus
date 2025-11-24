namespace NServiceBus.AcceptanceTests.Sagas;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTests;
using EndpointTemplates;
using NUnit.Framework;

public class When_handling_concurrent_messages : NServiceBusAcceptanceTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_not_overwrite_each_other(bool useOutbox)
    {
        // The true case provides a good test of the combination of Saga and Outbox together.
        if (useOutbox)
        {
            Requires.OutboxPersistence();
        }

        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithSagaAndOutbox>(b =>
            {
                b.DoNotFailOnErrorMessages();
                b.CustomConfig(cfg =>
                {
                    if (useOutbox)
                    {
                        cfg.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                        cfg.EnableOutbox();
                    }
                    cfg.Recoverability().Immediate(x => x.NumberOfRetries(5));
                });
                b.When((session, ctx) => session.SendLocal(new StartMsg { OrderId = "12345" }));
            })
            .Done(c => c.SagaData != null)
            .Run();

        Assert.That(context.SagaData, Is.Not.Null);
        Assert.That(context.SagaData.ContinueCount, Is.EqualTo(3));

        Assert.That(context.SagaData.CollectedIndexes, Does.Contain("1"));
        Assert.That(context.SagaData.CollectedIndexes, Does.Contain("2"));
        Assert.That(context.SagaData.CollectedIndexes, Does.Contain("3"));
    }

    public class Context : ScenarioContext
    {
        public EndpointWithSagaAndOutbox.OrderSagaData SagaData { get; set; }
    }

    public class EndpointWithSagaAndOutbox : EndpointConfigurationBuilder
    {
        public EndpointWithSagaAndOutbox() => EndpointSetup<DefaultServer>();

        class OrderSaga(Context testContext) : Saga<OrderSagaData>,
            IAmStartedByMessages<StartMsg>,
            IHandleMessages<ContinueMsg>,
            IHandleMessages<FinishMsg>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartMsg>(m => m.OrderId)
                    .ToMessage<ContinueMsg>(m => m.OrderId)
                    .ToMessage<FinishMsg>(m => m.OrderId);

            public async Task Handle(StartMsg message, IMessageHandlerContext context)
            {
                await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 1 });
                await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 2 });
                await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 3 });
            }

            public Task Handle(ContinueMsg message, IMessageHandlerContext context)
            {
                Data.ContinueCount++;
                Data.CollectedIndexes += message.Index.ToString();

                if (Data.ContinueCount == 3)
                {
                    return context.SendLocal(new FinishMsg { OrderId = message.OrderId });
                }

                return Task.CompletedTask;
            }

            public Task Handle(FinishMsg message, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.SagaData = Data;
                return Task.CompletedTask;
            }
        }

        public class OrderSagaData : ContainSagaData
        {
            public virtual string OrderId { get; set; }
            public virtual int ContinueCount { get; set; }
            public virtual string CollectedIndexes { get; set; }
        }
    }

    public class StartMsg : ICommand
    {
        public string OrderId { get; set; }
    }

    public class ContinueMsg : ICommand
    {
        public string OrderId { get; set; }
        public int Index { get; set; }
    }

    public class FinishMsg : ICommand
    {
        public string OrderId { get; set; }
    }
}