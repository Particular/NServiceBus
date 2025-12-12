namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[NServiceBusRegistrations]
public class When_manually_registering_saga_with_mapsaga_syntax : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_mapsaga_fluent_syntax_with_manually_registered_saga()
    {
        var orderId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<MapSagaSyntaxEndpoint>(b => b
                .When(session => session.SendLocal(new OrderPlaced { OrderId = orderId }))
                .When(c => c.OrderPlaced, session => session.SendLocal(new OrderPaid { OrderId = orderId })))
            .Done(c => c.OrderPaid)
            .Run();

        Assert.That(context.OrderPlaced, Is.True);
        Assert.That(context.OrderPaid, Is.True);
        Assert.That(context.OrderId, Is.EqualTo(orderId));
    }

    public class Context : ScenarioContext
    {
        public bool OrderPlaced { get; set; }
        public bool OrderPaid { get; set; }
        public Guid OrderId { get; set; }
    }

    public class MapSagaSyntaxEndpoint : EndpointConfigurationBuilder
    {
        public MapSagaSyntaxEndpoint() =>
            EndpointSetup<NonScanningServer>(config =>
            {
                config.AddSaga<FluentMappingSaga>();
            });

        public class FluentMappingSaga(Context testContext)
            : Saga<FluentMappingSagaData>,
              IAmStartedByMessages<OrderPlaced>,
              IHandleMessages<OrderPaid>
        {
            public Task Handle(OrderPlaced message, IMessageHandlerContext context)
            {
                testContext.OrderPlaced = true;
                testContext.OrderId = Data.OrderId;
                return Task.CompletedTask;
            }

            public Task Handle(OrderPaid message, IMessageHandlerContext context)
            {
                testContext.OrderPaid = true;
                MarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FluentMappingSagaData> mapper) =>
                mapper.MapSaga(saga => saga.OrderId)
                    .ToMessage<OrderPlaced>(msg => msg.OrderId)
                    .ToMessage<OrderPaid>(msg => msg.OrderId);
        }

        public class FluentMappingSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }
    }

    public class OrderPlaced : ICommand
    {
        public Guid OrderId { get; set; }
    }

    public class OrderPaid : ICommand
    {
        public Guid OrderId { get; set; }
    }
}