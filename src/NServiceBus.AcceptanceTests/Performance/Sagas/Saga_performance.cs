namespace NServiceBus.AcceptanceTests.Performance.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class Saga_performance : NServiceBusPerformanceTest
    {
        [Test, Ignore("Fails in build server! Not reliable")]
        public void With_dtc_enabled()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<SagaEndpoint>(SendTestMessages)
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(SagaPersisters.Default))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test, Ignore("Fails in build server! Not reliable")]
        public void With_dtc_supressed()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<SagaEndpoint>(b =>
                        {
                            b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DisableDistributedTransactions()));
                            SendTestMessages(b);
                        })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(SagaPersisters.Default))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        public class Context : PerformanceTestContext
        {

            public bool Complete { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = MaxConcurrencyLevel);
            }

            public class MySaga : Saga<MySaga.MySagaData>, IAmStartedByMessages<MyMessage>
            {
                public Context Context { get; set; }

                static int numberOfMessagesProcessed;


                public void Handle(MyMessage message)
                {
                    Data.SomeId = message.SomeId;

                    var current = Interlocked.Increment(ref numberOfMessagesProcessed);

                    if (current == 1)
                    {
                        Context.FirstMessageProcessedAt = DateTime.UtcNow;
                    }

                    if (current == Context.NumberOfTestMessages)
                    {
                        Context.LastMessageProcessedAt = DateTime.UtcNow;
                        Context.Complete = true;
                    }

                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<MyMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);

                }


                public class MySagaData : IContainSagaData
                {
                    public virtual Guid Id { get; set; }
                    public virtual string Originator { get; set; }
                    public virtual string OriginalMessageId { get; set; }

                    [Unique]
                    public virtual Guid SomeId { get; set; }
                }

            }


        }

        [Serializable]
        public class MyMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }



        protected static void SendTestMessages(EndpointBehaviorBuilder<Context> b)
        {
            b.Given((bus, context) => Parallel.For(0, context.NumberOfTestMessages, (s, c) => bus.SendLocal(new MyMessage
                {
                    SomeId = Guid.NewGuid()
                })));
        }
    }




}