namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sagas_share_timeout_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_instance_that_requested_the_timeout()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSagaMessage
            {
                Id = Guid.NewGuid().ToString()
            })))
            .Done(c => c.Saga1ReceivedTimeout || c.Saga2ReceivedTimeout)
            .Run(TimeSpan.FromSeconds(30));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Saga2ReceivedTimeout, Is.True);
            Assert.That(context.Saga1ReceivedTimeout, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool Saga1ReceivedTimeout { get; set; }
        public bool Saga2ReceivedTimeout { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class TimeoutSharingSaga1(Context testContext) : Saga<TimeoutSharingSaga1.TimeoutSharingSagaData1>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleTimeouts<MySagaTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSharingSagaData1> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSagaMessage>(m => m.Id);

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Timeout(MySagaTimeout state, IMessageHandlerContext context)
            {
                testContext.Saga1ReceivedTimeout = true;
                return Task.CompletedTask;
            }

            public class TimeoutSharingSagaData1 : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }

        public class TimeoutSharingSaga2(Context testContext) : Saga<TimeoutSharingSaga2.TimeoutSharingSagaData2>,
            IAmStartedByMessages<StartSagaMessage>, IHandleTimeouts<MySagaTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSharingSagaData2> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSagaMessage>(m => m.Id);

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => RequestTimeout<MySagaTimeout>(context, TimeSpan.FromSeconds(10));

            public Task Timeout(MySagaTimeout state, IMessageHandlerContext context)
            {
                testContext.Saga2ReceivedTimeout = true;
                return Task.CompletedTask;
            }
            public class TimeoutSharingSagaData2 : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
            }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public string Id { get; set; }
    }

    public class MySagaTimeout;
}