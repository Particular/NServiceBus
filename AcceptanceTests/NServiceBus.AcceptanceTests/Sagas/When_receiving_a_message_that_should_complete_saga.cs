namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading;

    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_a_message_that_completes_the_saga : NServiceBusIntegrationTest
    {
        static Guid IdThatSagaIsCorrelatedOn = Guid.NewGuid();
        static Guid IdThatSagaIsCorrelatedOn2 = Guid.NewGuid();

        [Test]
        public void Should_hydrate_and_complete_the_existing_instance()
        {
            Scenario.Define(() => new Context { SendAnotherMessage = false })
                    .WithEndpoint<SagaEndpoint>()
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                    {
                        Assert.IsNull(c.UnhandledException);
                        Assert.AreEqual(1, c.CompletedCount);
                    })

                    .Run();
        }

        [Test]
        [Ignore("Fails Currently. See https://github.com/NServiceBus/NServiceBus/issues/965")]
        public void Should_ignore_messages_afterwards()
        {
            Scenario.Define(() => new Context { SendAnotherMessage = true })
                    .WithEndpoint<SagaEndpoint>()
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                    {
                        Assert.IsNull(c.UnhandledException);
                        Assert.AreEqual(2, c.CompletedCount);
                        Assert.AreEqual(0, c.OtherMessageCount, "AnotheeMessage should not be delivered to the saga after completion");
                    })

                    .Run();
        }

        public class Context : ScenarioContext
        {
            public int CompletedCount { get; set; }
            public bool SendAnotherMessage { get; set; }
            public int OtherMessageCount { get; set; }
            public bool Complete { get; set; }
            public Exception UnhandledException { get; set; }
            public ManualResetEvent synchronizationEvent = new ManualResetEvent(false);
        }

        public class SagaEndpoint : EndpointBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.RavenSagaPersister().UnicastBus().LoadMessageHandlers<First<TestSaga>>())
                    .When<Context>((bus, context) =>
                        {
                            try
                            {
                                bus.SendLocal(new StartSagaMessage { SomeId = IdThatSagaIsCorrelatedOn });
                                WaitForSynchronizationEvent(context, "First StartSagaMessage not received");

                                bus.SendLocal(new CompleteSagaMessage { SomeId = IdThatSagaIsCorrelatedOn });
                                WaitForSynchronizationEvent(context, "First CompleteSagaMessage not received");

                                if (context.SendAnotherMessage)
                                {
                                    bus.SendLocal(new StartSagaMessage { SomeId = IdThatSagaIsCorrelatedOn2 });
                                    WaitForSynchronizationEvent(context, "Second StartSagaMessage not received");

                                    bus.SendLocal(new CompleteSagaMessage { SomeId = IdThatSagaIsCorrelatedOn2 });
                                    WaitForSynchronizationEvent(context, "Second CompleteSagaMessage not received");

                                    bus.SendLocal(new AnotherMessage { SomeId = IdThatSagaIsCorrelatedOn });
                                    WaitForSynchronizationEvent(context, "First AnotherMessage not received");

                                    bus.SendLocal(new AnotherMessage { SomeId = IdThatSagaIsCorrelatedOn2 });
                                    WaitForSynchronizationEvent(context, "Second AnotherMessage not received");
                                }
                            }
                            finally
                            {
                                context.Complete = true;
                            }
                        });
            }

            static void WaitForSynchronizationEvent(Context context, string message)
            {
                if (!context.synchronizationEvent.WaitOne(15000))
                {
                    Assert.Fail(message);
                }

                context.synchronizationEvent.Reset();
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>, IHandleMessages<AnotherMessage>
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
                    ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    ConfigureMapping<AnotherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(CompleteSagaMessage message)
                {
                    Context.CompletedCount = Context.CompletedCount + 1;
                    this.MarkAsComplete();
                    Context.synchronizationEvent.Set();
                }

                public void Handle(AnotherMessage message)
                {
                    Context.OtherMessageCount = Context.OtherMessageCount + 1;
                }

            }

            public class TestSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
                public Guid SomeId { get; set; }
            }
        }

        public class CompletionHandler : IHandleMessages<AnotherMessage>
        {
            public Context Context { get; set; }
            public void Handle(AnotherMessage message)
            {
                Context.synchronizationEvent.Set();
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class CompleteSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class AnotherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}