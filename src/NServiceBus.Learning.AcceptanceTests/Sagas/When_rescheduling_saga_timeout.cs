namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_rescheduling_saga_timeout : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_fire_once_with_the_rescheduled_payload()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.TimeoutValue is not null)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TimeoutValue, Is.EqualTo("Updated"));
            Assert.That(context.TimeoutsFired, Is.EqualTo(1));
        }
    }

    public class Context : ScenarioContext
    {
        public string TimeoutValue { get; set; }
        public int TimeoutsFired { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class RescheduleTimeoutSaga(Context testContext) : Saga<RescheduleTimeoutSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleMessages<RescheduleTimeout>,
            IHandleTimeouts<MyTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<RescheduleTimeout>(m => m.Id);

            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                // The original trigger fires before the rescheduled one, so it must be recognized as stale
                await RequestTimeout(context, TimeSpan.FromSeconds(2), new MyTimeout { Value = "Original" });
                await context.SendLocal(new RescheduleTimeout { Id = message.Id });
            }

            public async Task Handle(RescheduleTimeout message, IMessageHandlerContext context)
            {
                var timeout = (await FindTimeouts<MyTimeout>(_ => true, context.CancellationToken)).Single();
                timeout.Payload.Value = "Updated";
                await RescheduleTimeout(timeout.TimeoutId, DateTimeOffset.UtcNow.AddSeconds(5), context.CancellationToken);
            }

            public Task Timeout(MyTimeout state, IMessageHandlerContext context)
            {
                testContext.TimeoutsFired++;
                testContext.TimeoutValue = state.Value;
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

    public class RescheduleTimeout : ICommand
    {
        public string Id { get; set; }
    }

    public class MyTimeout
    {
        public string Value { get; set; }
    }
}
