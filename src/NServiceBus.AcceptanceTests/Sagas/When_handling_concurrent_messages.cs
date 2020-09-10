namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
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
                            cfg.EnableOutbox();
                        }
                        cfg.Recoverability().Immediate(x => x.NumberOfRetries(5));
                    });
                    b.When((session, ctx) => session.SendLocal(new StartMsg { OrderId = "12345" }));

                    var timeout = DateTime.UtcNow.AddSeconds(15);

                    b.When(c => DateTime.UtcNow > timeout, (session, ctx) => session.SendLocal(new FinishMsg { OrderId = "12345" }));
                })
                .Done(c => c.SagaData != null)
                .Run();

            Assert.IsNotNull(context.SagaData);
            Assert.AreEqual(3, context.SagaData.ContinueCount);

            StringAssert.Contains("1", context.SagaData.CollectedIndexes);
            StringAssert.Contains("2", context.SagaData.CollectedIndexes);
            StringAssert.Contains("3", context.SagaData.CollectedIndexes);
        }

        public class Context : ScenarioContext
        {
            public EndpointWithSagaAndOutbox.OrderSagaData SagaData { get; set; }
        }

        public class EndpointWithSagaAndOutbox : EndpointConfigurationBuilder
        {
            public EndpointWithSagaAndOutbox()
            {
                EndpointSetup<DefaultServer>();
            }

            class OrderSaga : Saga<OrderSagaData>,
                IAmStartedByMessages<StartMsg>,
                IHandleMessages<ContinueMsg>,
                IHandleMessages<FinishMsg>
            {
                public OrderSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                    mapper.ConfigureMapping<ContinueMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                    mapper.ConfigureMapping<FinishMsg>(m => m.OrderId).ToSaga(s => s.OrderId);
                }

                public async Task Handle(StartMsg message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 1 });
                    await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 2 });
                    await context.SendLocal(new ContinueMsg { OrderId = message.OrderId, Index = 3 });
                }

                public Task Handle(ContinueMsg message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    this.Data.ContinueCount++;
                    this.Data.CollectedIndexes += message.Index.ToString();

                    if (this.Data.ContinueCount == 3)
                    {
                        return context.SendLocal(new FinishMsg { OrderId = message.OrderId });
                    }

                    return Task.FromResult(0);
                }

                public Task Handle(FinishMsg message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    this.MarkAsComplete();
                    testContext.SagaData = this.Data;
                    return Task.FromResult(0);
                }

                Context testContext;
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
}