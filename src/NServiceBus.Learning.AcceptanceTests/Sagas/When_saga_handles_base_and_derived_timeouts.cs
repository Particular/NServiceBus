namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_handles_base_and_derived_timeouts : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_fire_only_the_handler_of_the_exact_timeout_type()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.DerivedTimeoutsFired > 0)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.DerivedTimeoutsFired, Is.EqualTo(1));
            Assert.That(context.BaseTimeoutsFired, Is.Zero);
        }
    }

    public class Context : ScenarioContext
    {
        public int BaseTimeoutsFired { get; set; }
        public int DerivedTimeoutsFired { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class BaseAndDerivedTimeoutSaga(Context testContext) : Saga<BaseAndDerivedTimeoutSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleTimeouts<BaseTimeout>,
            IHandleTimeouts<DerivedTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSaga>(m => m.Id);

            public Task Handle(StartSaga message, IMessageHandlerContext context) =>
                RequestTimeout(context, TimeSpan.FromSeconds(1), new DerivedTimeout());

            public Task Timeout(BaseTimeout state, IMessageHandlerContext context)
            {
                testContext.BaseTimeoutsFired++;
                return Task.CompletedTask;
            }

            public Task Timeout(DerivedTimeout state, IMessageHandlerContext context)
            {
                testContext.DerivedTimeoutsFired++;
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

    public class BaseTimeout;

    public class DerivedTimeout : BaseTimeout;
}
