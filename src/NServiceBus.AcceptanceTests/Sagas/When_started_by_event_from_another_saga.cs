namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    //Repro for #1323
    public class When_started_by_event_from_another_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_start_the_saga_and_request_a_timeout()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaThatPublishesAnEvent>(b =>
                    b.When(c => c.IsEventSubscriptionReceived,
                        session => session.SendLocal(new StartSaga
                        {
                            DataId = Guid.NewGuid()
                        }))
                )
                .WithEndpoint<SagaThatIsStartedByTheEvent>(
                    b => b.When(async (session, c) =>
                    {
                        await session.Subscribe<SomethingHappenedEvent>();

                        if (c.HasNativePubSubSupport)
                        {
                            c.IsEventSubscriptionReceived = true;
                        }
                    }))
                .Done(c => c.DidSaga1Complete && c.DidSaga2Complete)
                .Run();

            Assert.True(context.DidSaga1Complete && context.DidSaga2Complete);
        }

        public class Context : ScenarioContext
        {
            public bool DidSaga1Complete { get; set; }
            public bool DidSaga2Complete { get; set; }
            public bool IsEventSubscriptionReceived { get; set; }
        }

        public class SagaThatPublishesAnEvent : EndpointConfigurationBuilder
        {
            public SagaThatPublishesAnEvent()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.OnEndpointSubscribed<Context>((s, context) => { context.IsEventSubscriptionReceived = true; });
                });
            }

            public class EventFromOtherSaga1 : Saga<EventFromOtherSaga1.EventFromOtherSaga1Data>,
                IAmStartedByMessages<StartSaga>,
                IHandleTimeouts<EventFromOtherSaga1.Timeout1>
            {
                public EventFromOtherSaga1(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;

                    //Publish the event, which will start the second saga
                    await context.Publish<SomethingHappenedEvent>(m => { m.DataId = message.DataId; });

                    //Request a timeout
                    await RequestTimeout<Timeout1>(context, TimeSpan.FromMilliseconds(1));
                }

                public Task Timeout(Timeout1 state, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.DidSaga1Complete = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EventFromOtherSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class EventFromOtherSaga1Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                public class Timeout1
                {
                }

                Context testContext;
            }
        }

        public class SagaThatIsStartedByTheEvent : EndpointConfigurationBuilder
        {
            public SagaThatIsStartedByTheEvent()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.DisableFeature<AutoSubscribe>();
                },
                metadata => metadata.RegisterPublisherFor<SomethingHappenedEvent>(typeof(SagaThatPublishesAnEvent)));
            }

            public class EventFromOtherSaga2 : Saga<EventFromOtherSaga2.EventFromOtherSaga2Data>,
                IAmStartedByMessages<SomethingHappenedEvent>,
                IHandleTimeouts<EventFromOtherSaga2.Saga2Timeout>
            {
                public EventFromOtherSaga2(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SomethingHappenedEvent message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    //Request a timeout
                    return RequestTimeout<Saga2Timeout>(context, TimeSpan.FromMilliseconds(1));
                }

                public Task Timeout(Saga2Timeout state, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.DidSaga2Complete = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EventFromOtherSaga2Data> mapper)
                {
                    mapper.ConfigureMapping<SomethingHappenedEvent>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class EventFromOtherSaga2Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                public class Saga2Timeout
                {
                }

                Context testContext;
            }
        }


        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        public interface SomethingHappenedEvent : BaseEvent
        {
        }

        public interface BaseEvent : IEvent
        {
            Guid DataId { get; set; }
        }
    }
}