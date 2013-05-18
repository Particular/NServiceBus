﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_using_a_received_message_for_timeout : NServiceBusAcceptanceTest
    {
        [Test, Ignore("Not working!")]
        public void Timeout_should_be_received_after_expiration()
        {
            Scenario.Define(() => new Context {Id = Guid.NewGuid()})
                    .WithEndpoint<SagaEndpoint>(b =>
                        {
                            b.Given((bus, context) => bus.SendLocal(new StartSagaMessage {SomeId = context.Id}));

                            b.When(context => context.StartSagaMessageReceived,
                                   (bus, context) =>
                                       {
                                           bus.EnsureSubscriptionMessagesHaveArrived();
                                           bus.Publish(new SomeEvent {SomeId = context.Id});
                                       });
                        })
                    .Done(c => c.TimeoutReceived)
                    .Repeat(r => r.For(Transports.Default))
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
                EndpointSetup<DefaultServer>(c => c.RavenSagaPersister()
                                                   .DefineHowManySubscriptionMessagesToWaitFor(1)
                                                   .UnicastBus())
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
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }

                [Unique]
                public Guid SomeId { get; set; }
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