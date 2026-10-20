namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_updating_timeout : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_trigger_the_timeout_with_updated_state()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSagaMessage
            {
                Id = Guid.NewGuid().ToString()
            })))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SagaReceivedTimeout, Is.True);
            Assert.That(context.TimeoutValue, Is.EqualTo("Updated"));
        }
    }

    public class Context : ScenarioContext
    {
        public bool SagaReceivedTimeout { get; set; }
        public string TimeoutValue { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class TimeoutUpdateSaga(Context testContext) : Saga<TimeoutUpdateSaga.TimeoutUpdateSagaData>,
            IAmStartedByMessages<StartSagaMessage>,
            IAmStartedByMessages<RescheduleTimeoutMessage>,
            IAmStartedByMessages<UpdateTimeoutMessage>,
            IHandleTimeouts<MySagaTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutUpdateSagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSagaMessage>(m => m.Id)
                    .ToMessage<RescheduleTimeoutMessage>(m => m.Id)
                    .ToMessage<UpdateTimeoutMessage>(m => m.Id);

            public async Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                await RequestTimeout(context, TimeSpan.FromSeconds(10000),
                    new MySagaTimeout
                    {
                        Value = "Original"
                    });
                await context.SendLocal(new UpdateTimeoutMessage { Id = message.Id });
            }

            public async Task Handle(UpdateTimeoutMessage message, IMessageHandlerContext context)
            {
                var timeout = (await FindTimeouts<MySagaTimeout>(context, x => true))
                    .Single();
                await timeout.Update(new MySagaTimeout
                {
                    Value = "Updated"
                }, context);
                await context.SendLocal(new RescheduleTimeoutMessage { Id = message.Id });
            }

            public async Task Handle(RescheduleTimeoutMessage message, IMessageHandlerContext context)
            {
                var timeout = (await FindTimeouts<MySagaTimeout>(context, x => true))
                    .Single();
                await timeout.Reschedule(DateTimeOffset.UtcNow, context);
            }

            public Task Timeout(MySagaTimeout state, IMessageHandlerContext context)
            {
                testContext.TimeoutValue = state.Value;
                testContext.SagaReceivedTimeout = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            public class TimeoutUpdateSagaData : ContainSagaData, IContainSagaTimeoutData
            {
                public virtual string CorrelationProperty { get; set; }
                public List<TimeoutInfo> TimeoutData { get; set; } = [];
            }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public string Id { get; set; }
    }

    public class UpdateTimeoutMessage : ICommand
    {
        public string Id { get; set; }
    }

    public class RescheduleTimeoutMessage : ICommand
    {
        public string Id { get; set; }
    }

    public class MySagaTimeout
    {
        public string Value { get; set; }
    };
}