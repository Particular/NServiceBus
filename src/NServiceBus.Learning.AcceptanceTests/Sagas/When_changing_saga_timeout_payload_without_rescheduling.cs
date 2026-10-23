namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_changing_saga_timeout_payload_without_rescheduling : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_fire_with_the_transported_payload()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.TimeoutValue is not null)
            .Run();

        Assert.That(context.TimeoutValue, Is.EqualTo("Original"));
    }

    public class Context : ScenarioContext
    {
        public string TimeoutValue { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class ChangePayloadSaga(Context testContext) : Saga<ChangePayloadSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleMessages<ChangePayload>,
            IHandleTimeouts<MyTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<ChangePayload>(m => m.Id);

            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                await RequestTimeout(context, TimeSpan.FromSeconds(2), new MyTimeout { Value = "Original" });
                await context.SendLocal(new ChangePayload { Id = message.Id });
            }

            public async Task Handle(ChangePayload message, IMessageHandlerContext context)
            {
                var timeouts = await FindTimeouts<MyTimeout>(_ => true, context.CancellationToken);
                timeouts.Single().Payload.Value = "Changed";
            }

            public Task Timeout(MyTimeout state, IMessageHandlerContext context)
            {
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

    public class ChangePayload : ICommand
    {
        public string Id { get; set; }
    }

    public class MyTimeout
    {
        public string Value { get; set; }
    }
}
