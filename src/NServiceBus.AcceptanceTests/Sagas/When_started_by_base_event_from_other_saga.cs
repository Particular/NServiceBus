namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    //Repro for #1323
    public class When_started_by_base_event_from_other_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_start_the_saga_when_set_up_to_start_for_the_base_event()
        {
            var context = await Scenario.Define<SagaContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.IsEventSubscriptionReceived,
                        session => { return session.Publish<ISomethingHappenedEvent>(m => { m.DataId = Guid.NewGuid(); }); })
                )
                .WithEndpoint<SagaThatIsStartedByABaseEvent>(
                    b => b.When(async (session, c) =>
                    {
                        await session.Subscribe<IBaseEvent>();

                        if (c.HasNativePubSubSupport)
                        {
                            c.IsEventSubscriptionReceived = true;
                        }
                    }))
                .Done(c => c.DidSagaComplete)
                .Run();

            Assert.True(context.DidSagaComplete);
        }

        public class SagaContext : ScenarioContext
        {
            public bool IsEventSubscriptionReceived { get; set; }
            public bool DidSagaComplete { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<SagaContext>((s, context) =>
                {
                    context.AddTrace($"Subscription received for {s.SubscriberEndpoint}");
                    context.IsEventSubscriptionReceived = true;
                }));
            }
        }

        public class SagaThatIsStartedByABaseEvent : EndpointConfigurationBuilder
        {
            public SagaThatIsStartedByABaseEvent()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.DisableFeature<AutoSubscribe>();
                },
                metadata => metadata.RegisterPublisherFor<IBaseEvent>(typeof(Publisher)));
            }

            public class SagaStartedByBaseEvent : Saga<SagaStartedByBaseEvent.SagaStartedByBaseEventSagaData>, IAmStartedByMessages<IBaseEvent>
            {
                public SagaStartedByBaseEvent(SagaContext context)
                {
                    testContext = context;
                }

                public Task Handle(IBaseEvent message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    MarkAsComplete();
                    testContext.DidSagaComplete = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaStartedByBaseEventSagaData> mapper)
                {
                    mapper.ConfigureMapping<IBaseEvent>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class SagaStartedByBaseEventSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                SagaContext testContext;
            }
        }


        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        public interface ISomethingHappenedEvent : IBaseEvent
        {
        }

        public interface IBaseEvent : IEvent
        {
            Guid DataId { get; set; }
        }
    }
}