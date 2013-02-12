namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading;

    using EndpointTemplates;
    using AcceptanceTesting;
    using Logging;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_a_message_that_completes_the_saga : NServiceBusIntegrationTest
    {
        [Test]
        public void Should_hydrate_and_complete_the_existing_instance()
        {
            Scenario.Define(() => new Context { SendAnotherMessage = false, Id = Guid.NewGuid() })
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
        public void Should_ignore_messages_afterwards()
        {
            Scenario.Define(() => new Context {SendAnotherMessage = true, Id = Guid.NewGuid()})
                    .WithEndpoint<SagaEndpoint>()
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                        {
                            Assert.IsNull(c.UnhandledException);
                            Assert.AreEqual(1, c.CompletedCount);
                            Assert.AreEqual(0, c.OtherMessageCount,
                                            "AnotherMessage should not be delivered to the saga after completion");
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
            public readonly ManualResetEvent synchronizationEvent = new ManualResetEvent(false);
            public Guid Id { get; set; }
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
                                bus.SendLocal(new StartSagaMessage { SomeId = context.Id });
                                WaitForSynchronizationEvent(context, "First StartSagaMessage not received");

                                bus.SendLocal(new CompleteSagaMessage { SomeId = context.Id });
                                WaitForSynchronizationEvent(context, "First CompleteSagaMessage not received");

                                if (context.SendAnotherMessage)
                                {
                                    bus.SendLocal(new AnotherMessage { SomeId = context.Id });
                                    WaitForSynchronizationEvent(context, "First AnotherMessage not received");
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
                private ILog Logger = LogManager.GetLogger(typeof (TestSaga));
                
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Logger.WarnFormat("StartSagaMessage Context.Id = {0}", Context.Id);
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
                    Logger.WarnFormat("CompleteSagaMessage Context.Id = {0}", Context.Id);

                    Context.CompletedCount = Context.CompletedCount + 1;
                    MarkAsComplete();
                    Context.synchronizationEvent.Set();
                }

                public void Handle(AnotherMessage message)
                {
                    Logger.WarnFormat("AnotherMessage Context.Id = {0}", Context.Id);

                    Context.OtherMessageCount = Context.OtherMessageCount + 1;
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