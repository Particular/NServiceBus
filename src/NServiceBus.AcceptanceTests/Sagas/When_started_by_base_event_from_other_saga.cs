namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using PubSub;
    using Saga;
    using ScenarioDescriptors;

    //Repro for #1323
    public class When_started_by_base_event_from_other_saga : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_start_the_saga_when_set_up_to_start_for_the_base_event()
        {
            Scenario.Define<SagaContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.IsEventSubscriptionReceived,
                        bus =>
                            bus.Publish<SomethingHappenedEvent>(m => { m.DataId = Guid.NewGuid(); }))
                )
                .WithEndpoint<SagaThatIsStartedByABaseEvent>(
                    b => b.Given((bus, context) =>
                    {
                        bus.Subscribe<BaseEvent>();

                        if (context.HasNativePubSubSupport)
                            context.IsEventSubscriptionReceived = true;
                    }))
                .Done(c => c.DidSagaComplete)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.True(c.DidSagaComplete))
                .Run();
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
                    context.AddTrace("Subscription received for " + s.SubscriberReturnAddress.Queue);
                    context.IsEventSubscriptionReceived = true;
                }));
            }
        }

        public class SagaThatIsStartedByABaseEvent : EndpointConfigurationBuilder
        {
            public SagaThatIsStartedByABaseEvent()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<BaseEvent>(typeof(Publisher));
            }

            public class SagaStartedByBaseEvent : Saga<SagaStartedByBaseEvent.SagaData>, IAmStartedByMessages<BaseEvent>
            {
                public SagaContext Context { get; set; }

                public void Handle(BaseEvent message)
                {
                    Data.DataId = message.DataId;
                    MarkAsComplete();
                    Context.DidSagaComplete = true;
                }

                public class SagaData : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
                {
                }
            }
        }

        [Serializable]
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
