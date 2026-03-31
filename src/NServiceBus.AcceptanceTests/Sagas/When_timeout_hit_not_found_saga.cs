namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_timeout_hit_not_found_saga : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_fire_notfound_for_tm()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new StartSaga { DataId = Guid.NewGuid() })))
            .Run();

        Assert.That(context.NotFoundHandlerCalledForTimeout, Is.False);
    }

    public class Context : ScenarioContext
    {
        public bool NotFoundHandlerCalledForRegularMessage { get; set; }
        public bool NotFoundHandlerCalledForTimeout { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class TimeoutHitsNotFoundSaga
            : Saga<TimeoutHitsNotFoundSaga.TimeoutHitsNotFoundSagaData>,
                IAmStartedByMessages<StartSaga>,
                IHandleTimeouts<TimeoutHitsNotFoundSaga.MyTimeout>,
                IHandleMessages<SomeOtherMessage>
        {
            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;

                //this will cause the message to be delivered right away
                await RequestTimeout<MyTimeout>(context, TimeSpan.Zero);
                await context.SendLocal(new SomeOtherMessage { DataId = Guid.NewGuid() });

                MarkAsComplete();
            }

            public Task Handle(SomeOtherMessage message, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Timeout(MyTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutHitsNotFoundSagaData> mapper)
            {
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga>(m => m.DataId)
                    .ToMessage<SomeOtherMessage>(m => m.DataId);

                mapper.ConfigureNotFoundHandler<NotFoundHandler>();
            }

            class NotFoundHandler(Context testContext) : ISagaNotFoundHandler
            {
                public Task Handle(object message, IMessageProcessingContext context)
                {
                    if (message is SomeOtherMessage)
                    {
                        testContext.NotFoundHandlerCalledForRegularMessage = true;
                        testContext.MarkAsCompleted();
                    }

                    if (message is MyTimeout)
                    {
                        testContext.NotFoundHandlerCalledForTimeout = true;
                    }

                    return Task.CompletedTask;
                }
            }

            public class TimeoutHitsNotFoundSagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }

            public class MyTimeout;
        }
    }

    public class StartSaga : IMessage
    {
        public Guid DataId { get; set; }
    }

    public class SomeOtherMessage : IMessage
    {
        public Guid DataId { get; set; }
    }
}