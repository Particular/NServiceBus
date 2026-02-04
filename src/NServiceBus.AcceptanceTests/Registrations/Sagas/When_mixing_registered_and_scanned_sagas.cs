namespace NServiceBus.AcceptanceTests.Registrations.Sagas;

using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_mixing_registered_and_scanned_sagas : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_both_manually_registered_and_scanned_sagas([Values] RegistrationApproach approach)
    {
        var manualId = Guid.NewGuid();
        var scannedId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<HybridSagaEndpoint>(b => b.CustomRegistrations(approach,
                    static config => config.AddSaga<HybridSagaEndpoint.ManuallyRegisteredOrderSaga>(),
                    static registry => registry.Registrations.Sagas.AddWhen_mixing_registered_and_scanned_sagas__HybridSagaEndpoint__ManuallyRegisteredOrderSaga())
                .When(session => session.SendLocal(new StartManualSaga { OrderId = manualId }))
                .When(session => session.SendLocal(new StartScannedSaga { PaymentId = scannedId })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ManualSagaInvoked, Is.True);
            Assert.That(context.ScannedSagaInvoked, Is.True);
            Assert.That(context.ManualOrderId, Is.EqualTo(manualId));
            Assert.That(context.ScannedPaymentId, Is.EqualTo(scannedId));
        }
    }

    [Test]
    public async Task Should_not_duplicate_when_manually_registering_already_scanned_saga([Values] RegistrationApproach approach)
    {
        var orderId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<DuplicateRegistrationEndpoint>(b => b.CustomRegistrations(approach,
                    static config => config.AddSaga<DuplicateRegistrationEndpoint.DuplicateRegistrationSaga>(),
                    static registry => registry.Registrations.Sagas.AddWhen_mixing_registered_and_scanned_sagas__DuplicateRegistrationEndpoint__DuplicateRegistrationSaga())
                .When(session => session.SendLocal(new StartDuplicateSaga { OrderId = orderId })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.DuplicateSagaInvoked, Is.True);
            Assert.That(context.DuplicateOrderId, Is.EqualTo(orderId));
        }
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

        public void MaybeMarkAsComplete() => MarkAsCompleted(ManualSagaInvoked, ScannedSagaInvoked);
    }

    public class HybridSagaEndpoint : EndpointConfigurationBuilder
    {
        public HybridSagaEndpoint() => EndpointSetup<NonScanningServer>().IncludeType<ScannedPaymentSaga>();

        [Saga]
        public class ManuallyRegisteredOrderSaga(Context testContext)
            : Saga<ManuallyRegisteredOrderSagaData>, IAmStartedByMessages<StartManualSaga>
        {
            public Task Handle(StartManualSaga message, IMessageHandlerContext context)
            {
                testContext.ManualSagaInvoked = true;
                testContext.ManualOrderId = Data.OrderId;
                testContext.MaybeMarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ManuallyRegisteredOrderSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartManualSaga>(m => m.OrderId);
        }

        public class ManuallyRegisteredOrderSagaData : ContainSagaData
        {
            public virtual Guid OrderId { get; set; }
        }

#pragma warning disable NSB0025
        public class ScannedPaymentSaga(Context testContext)
#pragma warning restore NSB0025
            : Saga<ScannedPaymentSagaData>, IAmStartedByMessages<StartScannedSaga>
        {
            public Task Handle(StartScannedSaga message, IMessageHandlerContext context)
            {
                testContext.ScannedSagaInvoked = true;
                testContext.ScannedPaymentId = Data.PaymentId;
                testContext.MaybeMarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ScannedPaymentSagaData> mapper) =>
                mapper.MapSaga(s => s.PaymentId)
                    .ToMessage<StartScannedSaga>(m => m.PaymentId);
        }

        public class ScannedPaymentSagaData : ContainSagaData
        {
            public virtual Guid PaymentId { get; set; }
        }
    }

    public class DuplicateRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public DuplicateRegistrationEndpoint() => EndpointSetup<NonScanningServer>().IncludeType<DuplicateRegistrationSaga>();

        [Saga]
        public class DuplicateRegistrationSaga(Context testContext)
            : Saga<DuplicateRegistrationSagaData>, IAmStartedByMessages<StartDuplicateSaga>
        {
            public Task Handle(StartDuplicateSaga message, IMessageHandlerContext context)
            {
                testContext.DuplicateSagaInvoked = true;
                testContext.DuplicateOrderId = Data.OrderId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<DuplicateRegistrationSagaData> mapper) =>
                mapper.MapSaga(s => s.OrderId)
                    .ToMessage<StartDuplicateSaga>(m => m.OrderId);
        }

        public class DuplicateRegistrationSagaData : ContainSagaData
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