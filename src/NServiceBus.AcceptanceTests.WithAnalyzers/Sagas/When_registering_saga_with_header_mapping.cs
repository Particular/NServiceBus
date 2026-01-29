namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_registering_saga_with_header_mapping : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_correlate_using_header_with_manually_registered_saga([Values] RegistrationApproach approach)
    {
        string correlationId = Guid.NewGuid().ToString();

        Context context = await Scenario.Define<Context>()
            .WithEndpoint<HeaderMappingSagaEndpoint>(b => b.CustomRegistrations(approach,
                    static config => config.AddSaga<HeaderMappingSagaEndpoint.HeaderCorrelationSaga>(),
                    static registry => registry.Sagas.AddWhen_registering_saga_with_header_mapping__HeaderMappingSagaEndpoint__HeaderCorrelationSaga())
                .When(async session =>
                {
                    var options = new SendOptions();
                    options.RouteToThisEndpoint();
                    options.SetHeader("X-Correlation-Id", correlationId);
                    await session.Send(new StartWithHeader(), options);
                }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SagaWasInvoked, Is.True);
            Assert.That(context.CorrelationId, Is.EqualTo(correlationId));
        }
    }

    public class Context : ScenarioContext
    {
        public bool SagaWasInvoked { get; set; }
        public string CorrelationId { get; set; }
    }

    public class HeaderMappingSagaEndpoint : EndpointConfigurationBuilder
    {
        public HeaderMappingSagaEndpoint() => EndpointSetup<NonScanningServer>();

        [Saga]
        public class HeaderCorrelationSaga(Context testContext)
            : Saga<HeaderCorrelationSagaData>, IAmStartedByMessages<StartWithHeader>
        {
            public Task Handle(StartWithHeader message, IMessageHandlerContext context)
            {
                testContext.SagaWasInvoked = true;
                testContext.CorrelationId = Data.CorrelationId;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<HeaderCorrelationSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationId).ToMessageHeader<StartWithHeader>("X-Correlation-Id");
        }

        public class HeaderCorrelationSagaData : ContainSagaData
        {
            public virtual string CorrelationId { get; set; }
        }
    }

    public class StartWithHeader : ICommand;
}