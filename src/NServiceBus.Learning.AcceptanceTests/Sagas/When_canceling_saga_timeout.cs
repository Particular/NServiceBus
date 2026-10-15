namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_canceling_saga_timeout : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_fire_the_canceled_timeout()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.MarkerFired)
            .Run();

        Assert.That(context.CanceledTimeoutFired, Is.False);
    }

    public class Context : ScenarioContext
    {
        public bool CanceledTimeoutFired { get; set; }
        public bool MarkerFired { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class CancelTimeoutSaga(Context testContext) : Saga<CancelTimeoutSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleMessages<CancelTimeout>,
            IHandleTimeouts<CanceledTimeout>,
            IHandleTimeouts<MarkerTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<CancelTimeout>(m => m.Id);

            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                await RequestTimeout<CanceledTimeout>(context, TimeSpan.FromSeconds(2));
                await context.SendLocal(new CancelTimeout { Id = message.Id });
            }

            public async Task Handle(CancelTimeout message, IMessageHandlerContext context)
            {
                CancelTimeout(FindTimeouts<CanceledTimeout>(_ => true).Single().TimeoutId);

                // Fires after the canceled timeout would have, so the scenario observes its trigger being dropped
                await RequestTimeout<MarkerTimeout>(context, TimeSpan.FromSeconds(4));
            }

            public Task Timeout(CanceledTimeout state, IMessageHandlerContext context)
            {
                testContext.CanceledTimeoutFired = true;
                return Task.CompletedTask;
            }

            public Task Timeout(MarkerTimeout state, IMessageHandlerContext context)
            {
                testContext.MarkerFired = true;
                return Task.CompletedTask;
            }

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

    public class CancelTimeout : ICommand
    {
        public string Id { get; set; }
    }

    public class CanceledTimeout;

    public class MarkerTimeout;
}
