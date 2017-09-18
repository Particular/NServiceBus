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
                        session => { return session.Publish<SomethingHappenedEvent>(m => { m.DataId = Guid.NewGuid(); }); })
                )
                .WithEndpoint<SagaThatIsStartedByABaseEvent>(
                    b => b.When(async (session, c) =>
                    {
                        await session.Subscribe<BaseEvent>();

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
                metadata => metadata.RegisterPublisherFor<BaseEvent>(typeof(Publisher)));
            }

            public class SagaStartedByBaseEvent : Saga<SagaStartedByBaseEvent.SagaStartedByBaseEventSagaData>, IAmStartedByMessages<BaseEvent>
            {
                public SagaContext Context { get; set; }

                public Task Handle(BaseEvent message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    MarkAsComplete();
                    Context.DidSagaComplete = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaStartedByBaseEventSagaData> mapper)
                {
                    mapper.ConfigureMapping<BaseEvent>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class SagaStartedByBaseEventSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
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