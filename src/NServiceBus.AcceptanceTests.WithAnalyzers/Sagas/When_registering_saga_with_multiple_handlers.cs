namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_registering_saga_with_multiple_handlers : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_multiple_message_types_with_manually_registered_saga([Values] RegistrationApproach approach)
    {
        var orderId = Guid.NewGuid();

        Context context = await Scenario.Define<Context>()
            .WithEndpoint<MultiHandlerSagaEndpoint>(b => b.CustomRegistrations(approach,
                    static config => config.AddSaga<MultiHandlerSagaEndpoint.MultiMessageSaga>(),
                    static registry => registry.Sagas.AddWhen_registering_saga_with_multiple_handlers__MultiHandlerSagaEndpoint__MultiMessageSaga())
                .When(session => session.SendLocal(new StartOrder { OrderId = orderId }))
                .When(c => c.OrderStarted, session => session.SendLocal(new UpdateOrder { OrderId = orderId }))
                .When(c => c.OrderUpdated, session => session.SendLocal(new CompleteOrder { OrderId = orderId })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.OrderStarted, Is.True);
            Assert.That(context.OrderUpdated, Is.True);
            Assert.That(context.OrderCompleted, Is.True);
            Assert.That(context.OrderId, Is.EqualTo(orderId));
        }
    }

    public class Context : ScenarioContext
    {
        public bool OrderStarted { get; set; }
        public bool OrderUpdated { get; set; }
        public bool OrderCompleted { get; set; }
        public Guid OrderId { get; set; }
    }

    public class MultiHandlerSagaEndpoint : EndpointConfigurationBuilder
    {
        public MultiHandlerSagaEndpoint() => EndpointSetup<NonScanningServer>();

        [Saga]
        public class MultiMessageSaga(Context testContext)
            : Saga<MultiMessageSagaData>,
                IAmStartedByMessages<StartOrder>,
                IHandleMessages<UpdateOrder>,
                IHandleMessages<CompleteOrder>
        {
            public Task Handle(StartOrder message, IMessageHandlerContext context)
            {
                testContext.OrderStarted = true;
                testContext.OrderId = Data.OrderId;
                return Task.CompletedTask;
            }

            public Task Handle(CompleteOrder message, IMessageHandlerContext context)
            {
                testContext.OrderCompleted = true;
                MarkAsComplete();
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            public Task Handle(UpdateOrder message, IMessageHandlerContext context)
            {
                testContext.OrderUpdated = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MultiMessageSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartOrder>(m => m.OrderId)
                    .ToMessage<UpdateOrder>(m => m.OrderId)
                    .ToMessage<CompleteOrder>(m => m.OrderId);
        }

        public class MultiMessageSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }
    }

    public class StartOrder : ICommand
    {
        public Guid OrderId { get; set; }
    }

    public class UpdateOrder : ICommand
    {
        public Guid OrderId { get; set; }
    }

    public class CompleteOrder : ICommand
    {
        public Guid OrderId { get; set; }
    }
}