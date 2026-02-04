namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_mixing_manual_and_scanned_sagas : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_both_manually_registered_and_scanned_sagas()
    {
        var manualId = Guid.NewGuid();
        var scannedId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<HybridRegistrationEndpoint>(b => b
                .When(session => session.SendLocal(new StartManualSaga { OrderId = manualId }))
                .When(session => session.SendLocal(new StartScannedSaga { PaymentId = scannedId })))
            .Run();

        Assert.That(context.ManualSagaInvoked, Is.True);
        Assert.That(context.ScannedSagaInvoked, Is.True);
        Assert.That(context.ManualOrderId, Is.EqualTo(manualId));
        Assert.That(context.ScannedPaymentId, Is.EqualTo(scannedId));
    }

    [Test]
    public async Task Should_not_duplicate_when_manually_registering_already_scanned_saga()
    {
        var orderId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<DuplicatedRegistrationEndpoint>(b => b.When(session => session.SendLocal(new StartDuplicateSaga
            {
                OrderId = orderId
            })))
            .Run();

        Assert.That(context.DuplicateSagaInvoked, Is.True);
        Assert.That(context.DuplicateOrderId, Is.EqualTo(orderId));
        // The saga should only be invoked once, not twice
    }

    public class Context : ScenarioContext
    {
        public bool ManualSagaInvoked { get; set; }
        public bool ScannedSagaInvoked { get; set; }
        public Guid ManualOrderId { get; set; }
        public Guid ScannedPaymentId { get; set; }

        public bool DuplicateSagaInvoked { get; set; }
        public Guid DuplicateOrderId { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(ManualSagaInvoked, ScannedSagaInvoked);
    }

    public class HybridRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public HybridRegistrationEndpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.AddSaga<HybridManuallyRegisteredOrderSaga>();
            }).DoNotAutoRegisterHandlers().DoNotAutoRegisterSagas()
            .IncludeType<HybridScannedPaymentSaga>();

        [Saga]
        public class HybridManuallyRegisteredOrderSaga(Context testContext)
            : Saga<HybridManuallyRegisteredOrderSagaData>, IAmStartedByMessages<StartManualSaga>
        {
            public Task Handle(StartManualSaga message, IMessageHandlerContext context)
            {
                testContext.ManualSagaInvoked = true;
                testContext.ManualOrderId = Data.OrderId;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<HybridManuallyRegisteredOrderSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartManualSaga>(m => m.OrderId);
        }

        public class HybridManuallyRegisteredOrderSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }

        [Saga]
        public class HybridScannedPaymentSaga(Context testContext)
            : Saga<HybridScannedPaymentSagaData>, IAmStartedByMessages<StartScannedSaga>
        {
            public Task Handle(StartScannedSaga message, IMessageHandlerContext context)
            {
                testContext.ScannedSagaInvoked = true;
                testContext.ScannedPaymentId = Data.PaymentId;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<HybridScannedPaymentSagaData> mapper) =>
                mapper.MapSaga(s => s.PaymentId)
                    .ToMessage<StartScannedSaga>(m => m.PaymentId);
        }

        public class HybridScannedPaymentSagaData : ContainSagaData
        {
            public virtual Guid PaymentId { get; set; }
        }
    }

    public class DuplicatedRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public DuplicatedRegistrationEndpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.AddSaga<DuplicatedRegistrationSaga>();
            }).DoNotAutoRegisterHandlers().DoNotAutoRegisterSagas()
            .IncludeType<DuplicatedRegistrationSaga>();

        [Saga]
        public class DuplicatedRegistrationSaga(Context testContext)
            : Saga<DuplicatedRegistrationSagaData>, IAmStartedByMessages<StartDuplicateSaga>
        {
            public Task Handle(StartDuplicateSaga message, IMessageHandlerContext context)
            {
                testContext.DuplicateSagaInvoked = true;
                testContext.DuplicateOrderId = Data.OrderId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DuplicatedRegistrationSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartDuplicateSaga>(m => m.OrderId);
        }

        public class DuplicatedRegistrationSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }
    }

    public class StartManualSaga : ICommand
    {
        public Guid OrderId { get; set; }
    }

    public class StartScannedSaga : ICommand
    {
        public Guid PaymentId { get; set; }
    }

    public class StartDuplicateSaga : ICommand
    {
        public Guid OrderId { get; set; }
    }
}