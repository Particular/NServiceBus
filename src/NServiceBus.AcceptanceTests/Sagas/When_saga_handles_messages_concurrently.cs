using System;
using System.Threading.Tasks;

namespace NServiceBus.AcceptanceTests.Sagas
{
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_saga_handles_messages_concurrently
    {
        [Test]
        public Task Should_complete_saga_eventually()
        {
            return Scenario.Define<Context>(c => { c.OrderId = Guid.NewGuid().ToString(); })
                .WithEndpoint<ConcurrentHandlerEndpoint>(b =>
                {
                    b.When((session, context) =>
                    {
                        var t1 = session.SendLocal(new ConcurrentHandlerEndpoint.OrderPlaced { OrderId = context.OrderId });
                        var t2 = session.SendLocal(new ConcurrentHandlerEndpoint.OrderBilled { OrderId = context.OrderId });
                        return Task.WhenAll(t1, t2);
                    });
                })
                .Done(c => c.PlacedSagaId != Guid.Empty && c.BilledSagaId != Guid.Empty)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreNotEqual(Guid.Empty, c.PlacedSagaId);
                    Assert.AreNotEqual(Guid.Empty, c.BilledSagaId);
                    Assert.AreEqual(c.PlacedSagaId, c.BilledSagaId, "Both messages should have been handled by the same saga, but SagaIds don't match.");
                    Assert.True(c.SagaCompleted);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public string OrderId { get; set; }
            public Guid PlacedSagaId { get; set; }
            public Guid BilledSagaId { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class ConcurrentHandlerEndpoint : EndpointConfigurationBuilder
        {
            public ConcurrentHandlerEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.LimitMessageProcessingConcurrencyTo(2);
                    b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(3));
                });
            }

            public class OrderShippingPolicy : Saga<OrderBilledPolicyData>,
                IAmStartedByMessages<OrderBilled>,
                IAmStartedByMessages<OrderPlaced>
            {
                public Context Context { get; set; }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderBilledPolicyData> mapper)
                {
                    mapper.ConfigureMapping<OrderPlaced>(msg => msg.OrderId).ToSaga(saga => saga.OrderId);
                    mapper.ConfigureMapping<OrderBilled>(msg => msg.OrderId).ToSaga(saga => saga.OrderId);
                }

                public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
                {
                    Data.Placed = true;
                    await context.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(OrderPlaced)
                    });
                    await CheckForCompletion(context);
                }

                public async Task Handle(OrderBilled message, IMessageHandlerContext context)
                {
                    Data.Billed = true;
                    await context.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(OrderBilled)
                    });
                    await CheckForCompletion(context);
                }

                Task CheckForCompletion(IMessageHandlerContext context)
                {
                    if (Data.Billed && Data.Placed)
                    {
                        this.MarkAsComplete();
                        Context.SagaCompleted = true;
                    }
                    return Task.FromResult(0);
                }
            }

            public class OrderBilledPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
                public bool Placed { get; set; }
                public bool Billed { get; set; }
            }

            public class LogSuccessfulHandler : IHandleMessages<SuccessfulProcessing>
            {
                public Context Context { get; set; }

                public Task Handle(SuccessfulProcessing message, IMessageHandlerContext context)
                {
                    if (message.Type == nameof(OrderPlaced))
                    {
                        Context.PlacedSagaId = message.SagaId;
                    }
                    else if (message.Type == nameof(OrderBilled))
                    {
                        Context.BilledSagaId = message.SagaId;
                    }
                    else
                    {
                        throw new Exception("Unknown type");
                    }

                    return Task.FromResult(0);
                }
            }

            public class OrderPlaced : ICommand
            {
                public string OrderId { get; set; }
            }

            public class OrderBilled : ICommand
            {
                public string OrderId { get; set; }
            }

            public class SuccessfulProcessing : ICommand
            {
                public string Type { get; set; }
                public Guid SagaId { get; set; }
            }
        }
    }
}
