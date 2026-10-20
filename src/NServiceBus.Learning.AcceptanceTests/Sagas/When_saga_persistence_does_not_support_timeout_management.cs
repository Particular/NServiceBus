namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

// The analyzer can't tell which sagas of this assembly an endpoint hosts, so it warns here, which is the case the startup check covers
#pragma warning disable NSB0043

public class When_saga_persistence_does_not_support_timeout_management : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_fail_to_start_with_sagas_using_timeout_management()
    {
        var exception = await Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
            .WithEndpoint<EndpointWithSaga>()
            .Done(c => c.EndpointsStarted)
            .Run());

        Assert.That(exception.Message, Does.StartWith("The selected saga persistence 'AcceptanceTestingPersistence' doesn't support saga timeout management"));
        Assert.That(exception.Message, Does.Contain(typeof(EndpointWithSaga.CancelingSaga).FullName));
    }

    [Test]
    public async Task Should_fail_to_start_when_reporting_to_ServiceControl()
    {
        var exception = await Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
            .WithEndpoint<EndpointReportingTimeouts>()
            .Done(c => c.EndpointsStarted)
            .Run());

        Assert.That(exception.Message, Does.StartWith("Saga timeout reporting to ServiceControl requires a saga persistence that supports timeout management"));
    }

    public class EndpointWithSaga : EndpointConfigurationBuilder
    {
        public EndpointWithSaga() => EndpointSetup<DefaultServer>(c => c.UsePersistence<AcceptanceTestingPersistence, StorageType.Sagas>());

        [Saga]
        public class CancelingSaga : Saga<CancelingSaga.SagaData>, IAmStartedByMessages<StartSaga>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty).ToMessage<StartSaga>(m => m.Id);

            public Task Handle(StartSaga message, IMessageHandlerContext context) => CancelTimeout("timeout-id", context.CancellationToken);

            public class SagaData : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }
    }

    public class EndpointReportingTimeouts : EndpointConfigurationBuilder
    {
        public EndpointReportingTimeouts() => EndpointSetup<DefaultServer>(c =>
        {
            c.UsePersistence<AcceptanceTestingPersistence, StorageType.Sagas>();
            c.ServiceControlErrorInstanceQueue("ServiceControl");
#pragma warning disable NSB0042 // The analyzer reports this misconfiguration; the test covers the startup check behind it
            c.ReportSagaTimeoutsToServiceControl();
#pragma warning restore NSB0042
        });

        [Saga]
        public class SagaWithoutTimeoutManagement : Saga<SagaWithoutTimeoutManagement.SagaData>, IAmStartedByMessages<StartSaga>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty).ToMessage<StartSaga>(m => m.Id);

            public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            public class SagaData : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }
    }

    public class StartSaga : ICommand
    {
        public string Id { get; set; }
    }
}
