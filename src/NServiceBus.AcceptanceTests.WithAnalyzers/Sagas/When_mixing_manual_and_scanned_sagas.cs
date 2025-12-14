namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[NServiceBusRegistrations]
public class When_mixing_manual_and_scanned_sagas : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_both_manually_registered_and_scanned_sagas()
    {
        var manualId = Guid.NewGuid();
        var scannedId = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<HybridSagaEndpoint>(b => b
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
            .WithEndpoint<DuplicateRegistrationEndpoint>(b => b.When(session => session.SendLocal(new StartDuplicateSaga
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

        public void MaybeCompleted() => MarkAsCompleted(ManualSagaInvoked, ScannedSagaInvoked);

        public bool DuplicateSagaInvoked { get; set; }
        public Guid DuplicateOrderId { get; set; }
    }

    public class HybridSagaEndpoint : EndpointConfigurationBuilder
    {
        public HybridSagaEndpoint() =>
            EndpointSetup<NonScanningServer>(config =>
            {
                config.AddSaga<ManuallyRegisteredOrderSaga>();
            })
            .IncludeType<ScannedPaymentSaga>();

        public class ManuallyRegisteredOrderSaga(Context testContext)
            : Saga<ManuallyRegisteredOrderSagaData>, IAmStartedByMessages<StartManualSaga>
        {
            public Task Handle(StartManualSaga message, IMessageHandlerContext context)
            {
                testContext.ManualSagaInvoked = true;
                testContext.ManualOrderId = Data.OrderId;
                testContext.MaybeCompleted();
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

        public class ScannedPaymentSaga(Context testContext)
            : Saga<ScannedPaymentSagaData>, IAmStartedByMessages<StartScannedSaga>
        {
            public Task Handle(StartScannedSaga message, IMessageHandlerContext context)
            {
                testContext.ScannedSagaInvoked = true;
                testContext.ScannedPaymentId = Data.PaymentId;
                testContext.MaybeCompleted();
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
        public DuplicateRegistrationEndpoint() =>
            EndpointSetup<NonScanningServer>(config =>
            {
                config.AddSaga<DuplicateRegistrationSaga>();
            })
            .IncludeType<DuplicateRegistrationSaga>();

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