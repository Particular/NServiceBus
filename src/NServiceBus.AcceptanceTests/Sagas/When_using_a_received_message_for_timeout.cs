namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using PubSub;
    using Saga;

    public class When_using_a_received_message_for_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public void Timeout_should_be_received_after_expiration()
        {
            Scenario.Define(() => new Context {Id = Guid.NewGuid()})
                    .WithEndpoint<SagaEndpoint>(b =>
                    {
                        b.Given((bus, context) =>
                        {
                            if (context.HasSupportForCentralizedPubSub)
                            {
                                bus.SendLocal(new StartSagaMessage { SomeId = context.Id });
                            }
                            else
                            {
                                SubscriptionBehavior.OnEndpointSubscribed(s => bus.SendLocal(new StartSagaMessage { SomeId = context.Id }));
                            }

                        });

                        b.When(context => context.StartSagaMessageReceived,
                            (bus, context) => bus.Publish(new SomeEvent { SomeId = context.Id }));

                    })
                    .Done(c => c.TimeoutReceived)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public bool StartSagaMessageReceived { get; set; }

            public bool SomeEventReceived { get; set; }

            public bool TimeoutReceived { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<SomeEvent>(typeof (SagaEndpoint));
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>,
                                    IHandleMessages<SomeEvent>, IHandleTimeouts<SomeEvent>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;
                    Context.StartSagaMessageReceived = true;
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    ConfigureMapping<SomeEvent>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(SomeEvent message)
                {
                    RequestTimeout(TimeSpan.FromMilliseconds(100), message);
                    Context.SomeEventReceived = true;
                }

                public void Timeout(SomeEvent message)
                {
                    Context.TimeoutReceived = true;
                    MarkAsComplete();
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }

                [Unique]
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class SomeEvent : IEvent
        {
            public Guid SomeId { get; set; }
        }
    }
}