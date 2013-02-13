namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading;

    using EndpointTemplates;
    using AcceptanceTesting;

    using NServiceBus.Unicast.Subscriptions;

    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_using_a_received_message_for_timeout : NServiceBusIntegrationTest
    {
        [Test]
        public void Timeout_should_be_received_after_expiration()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<SagaEndpoint>()
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c => Assert.AreEqual(1, c.CompletedCount))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public int CompletedCount { get; set; }
            public bool Complete { get; set; }
            public readonly ManualResetEvent synchronizationEvent = new ManualResetEvent(false);
            public Guid Id { get; set; }
        }

        public class SagaEndpoint : EndpointBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.RavenSagaPersister()
                                                   .UnicastBus())
                    .AddMapping<SomeEvent>(typeof (SagaEndpoint))
                    .When<Context>((bus, context) =>
                        {
                            try
                            {
                                bus.SendLocal(new StartSagaMessage {SomeId = context.Id});
                                WaitForSynchronizationEvent(context, "StartSagaMessage not received");

                                PublishSomeEvent(bus, context);
                                WaitForSynchronizationEvent(context, "SomeEvent not received");

                                WaitForSynchronizationEvent(context, "Timeout not received");
                            }
                            finally
                            {
                                context.Complete = true;
                            }
                        });
            }

            static void PublishSomeEvent(IBus bus, Context context)
            {
                if (Configure.Instance.Configurer.HasComponent<MessageDrivenSubscriptionManager>())
                {
                    Configure.Instance.Builder.Build<MessageDrivenSubscriptionManager>().ClientSubscribed +=
                        (sender, args) => bus.Publish(new SomeEvent { SomeId = context.Id });
                }
                else
                {
                    bus.Publish(new SomeEvent { SomeId = context.Id });               
                }                
            }

            static void WaitForSynchronizationEvent(Context context, string message)
            {
                if (!context.synchronizationEvent.WaitOne(15000))
                {
                    Assert.Fail(message);
                }

                context.synchronizationEvent.Reset();
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<SomeEvent>, IHandleTimeouts<SomeEvent>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;
                    Context.synchronizationEvent.Set();
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<StartSagaMessage>(m=>m.SomeId)
                        .ToSaga(s=>s.SomeId);
                    ConfigureMapping<SomeEvent>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(SomeEvent message)
                {
                    RequestTimeout(TimeSpan.FromMilliseconds(100), message);
                    Context.synchronizationEvent.Set();
                }

                public void Timeout(SomeEvent message)
                {
                    Context.CompletedCount = Context.CompletedCount + 1;
                    Context.synchronizationEvent.Set();
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