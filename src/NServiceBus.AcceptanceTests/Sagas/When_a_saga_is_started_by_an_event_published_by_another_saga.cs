namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;
    using PubSub;
    using Saga;
    using ScenarioDescriptors;

    //Repro for #1323
    public class When_a_saga_is_started_by_an_event_published_by_another_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_start_the_saga_and_request_a_timeout()
        {
            Scenario.Define<Context>()
                .WithEndpoint<SagaThatPublishesAnEvent>(b =>
                    b.When(c => c.IsEventSubscriptionReceived,
                            bus =>
                                bus.SendLocal(new StartSaga
                                {
                                    DataId = Guid.NewGuid()
                                }))
                )
                .WithEndpoint<SagaThatIsStartedByTheEvent>(
                    b => b.Given((bus, context) =>
                    {
                        bus.Subscribe<SomethingHappenedEvent>();

                        if (context.HasNativePubSubSupport)
                            context.IsEventSubscriptionReceived = true;
                    }))

                .Done(c => c.DidSaga1Complete && c.DidSaga2Complete)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.True(c.DidSaga1Complete && c.DidSaga2Complete))
                .Run(new RunSettings
                {
                    UseSeparateAppDomains = true
                });
        }

        public class Context : ScenarioContext
        {
            public bool DidSaga1Complete { get; set; }
            public bool DidSaga2Complete { get; set; }
            public bool IsEventSubscriptionReceived { get; set; }
        }

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
                EndpointSetup<DefaultPublisher>(c => { }, b => b.OnEndpointSubscribed<SagaContext>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Queue.Contains("SagaThatIsStartedByABaseEvent"))
                    {
                        context.IsEventSubscriptionReceived = true;
                    }
                }));
            }
        }

        public class SagaThatPublishesAnEvent : EndpointConfigurationBuilder
        {
            public SagaThatPublishesAnEvent()
            {
                EndpointSetup<DefaultPublisher>(c => { }, b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Queue.Contains("SagaThatIsStartedByTheEvent"))
                    {
                        context.IsEventSubscriptionReceived = true;
                    }
                }));
            }

            public class Saga1 : Saga<Saga1.Saga1Data>, IAmStartedByMessages<StartSaga>, IHandleTimeouts<Saga1.Timeout1>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;

                    //Publish the event, which will start the second saga
                    Bus.Publish<SomethingHappenedEvent>(m => { m.DataId = message.DataId; });

                    //Request a timeout
                    RequestTimeout<Timeout1>(TimeSpan.FromSeconds(5));
                }

                public void Timeout(Timeout1 state)
                {
                    MarkAsComplete();
                    Context.DidSaga1Complete = true;
                }

                public class Saga1Data : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }

                public class Timeout1
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                }
            }
        }

        public class SagaThatIsStartedByTheEvent : EndpointConfigurationBuilder
        {
            public SagaThatIsStartedByTheEvent()
            {
                EndpointSetup<DefaultServer>(_ => { }, c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<SomethingHappenedEvent>(typeof(SagaThatPublishesAnEvent));

            }

            public class Saga2 : Saga<Saga2.Saga2Data>, IAmStartedByMessages<SomethingHappenedEvent>, IHandleTimeouts<Saga2.Saga2Timeout>
            {
                public Context Context { get; set; }

                public void Handle(SomethingHappenedEvent message)
                {
                    Data.DataId = message.DataId;

                    //Request a timeout
                    RequestTimeout<Saga2Timeout>(TimeSpan.FromSeconds(5));
                }

                public void Timeout(Saga2Timeout state)
                {
                    MarkAsComplete();
                    Context.DidSaga2Complete = true;
                }

                public class Saga2Data : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }

                public class Saga2Timeout
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga2Data> mapper)
                {
                }
            }
        }

        public class SagaThatIsStartedByABaseEvent : EndpointConfigurationBuilder
        {
            public SagaThatIsStartedByABaseEvent()
            {
                EndpointSetup<DefaultServer>(_ => { }, c => c.DisableFeature<AutoSubscribe>())
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
