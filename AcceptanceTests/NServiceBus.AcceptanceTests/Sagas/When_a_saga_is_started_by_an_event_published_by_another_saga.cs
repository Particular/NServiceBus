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
    public class When_a_saga_is_started_by_an_event_published_by_another_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_start_the_saga_and_request_a_timeout()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<SagaThatPublishesAnEvent>(b =>
                                                            b.Given(
                                                                (bus, context) =>
                                                                Subscriptions.OnEndpointSubscribed(s =>
                                                                    {
                                                                        if (s.SubscriberReturnAddress.Queue.Contains("SagaThatIsStartedByTheEvent"))
                                                                        {
                                                                            context.IsEventSubscriptionReceived = true;
                                                                        }
                                                                    }))
                                                             .When(c => c.IsEventSubscriptionReceived,
                                                                   bus =>
                                                                   bus.SendLocal(new StartSaga {DataId = Guid.NewGuid()}))
                )
                    .WithEndpoint<SagaThatIsStartedByTheEvent>(
                        b => b.Given((bus, context) => bus.Subscribe<SomethingHappenedEvent>()))

                    .Done(c => c.DidSaga1Complete && c.DidSaga2Complete)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.DidSaga1Complete && c.DidSaga2Complete))
                    .Run();
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
                EndpointSetup<DefaultServer>(c => Configure.Features.Disable<AutoSubscribe>());
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
                    public Guid DataId { get; set; }
                }

                public class Timeout1
                {
                }
            }
        }

        public class SagaThatIsStartedByTheEvent : EndpointConfigurationBuilder
        {
            public SagaThatIsStartedByTheEvent()
            {
                EndpointSetup<DefaultServer>(c => Configure.Features.Disable<AutoSubscribe>())
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
                    public Guid DataId { get; set; }
                }

                public class Saga2Timeout
                {
                }
            }
        }

        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        [Serializable]
        public class SomethingHappenedEvent : IEvent
        {
            public Guid DataId { get; set; }
        }
    }
}
