using System;

namespace Runner
{
   using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    using NHibernate.Cfg;

    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Saga;
    using NServiceBus.UnitOfWork;

    using Raven.Client;
    using Raven.Client.Document;

    using Configuration = System.Configuration.Configuration;

    class Program
    {
        static void Main(string[] args)
        {
            var numberOfThreads = int.Parse(args[0]);
            bool volatileMode = (args[4].ToLower() == "volatile");
            bool suppressDTC = (args[4].ToLower() == "suppressdtc");
            bool twoPhaseCommit = (args[4].ToLower() == "twophasecommit");
            bool writeToRaven = (args[5].ToLower() == "writetoraven");
            bool saga = (args[5].ToLower() == "sagamessages");

            volatileMode = volatileMode || writeToRaven;

            TransportConfigOverride.MaximumConcurrencyLevel = numberOfThreads;

            var numberOfMessages = int.Parse(args[1]);

            var endpointName = "PerformanceTest";

            if (volatileMode)
                endpointName += ".Volatile";

            if (suppressDTC)
                endpointName += ".SuppressDTC";

            var config = Configure.With()
                                  .DefineEndpointName(endpointName)
                                  .DefaultBuilder();

            if (writeToRaven)
            {
                var documentStore = new DocumentStore();
                documentStore.DefaultDatabase = "TestDB";
                documentStore.Url = "http://localhost:8080";
                documentStore.Initialize();

                config.Configurer.ConfigureComponent(() => documentStore.OpenSession(), DependencyLifecycle.InstancePerUnitOfWork);
                config.Configurer.ConfigureComponent<TestRavenUnitOfWork>(DependencyLifecycle.InstancePerCall);
            }

            switch (args[2].ToLower())
            {
                case "xml":
                    config.XmlSerializer();
                    break;
                    
                case "json":
                    config.JsonSerializer();
                    break;

                case "bson":
                    config.BsonSerializer();
                    break;

                case "bin":
                    config.BinarySerializer();
                    break;

                default:
                    throw new InvalidOperationException("Illegal serialization format " + args[2]);
            }

            if (saga)
            {
                config.Sagas().RavenSagaPersister();//.UseNHibernateSagaPersister();
            }

            if (volatileMode)
            {
                Configure.Endpoint.AsVolatile();
            }

            if (suppressDTC)
            {
                Configure.Transactions.Advanced(settings => settings.DisableDistributedTransactions());
            }

            switch (args[3].ToLower())
            {
                case "msmq":
                    config.UseTransport<Msmq>();
                    break;

                case "sqlserver":
                    config.UseTransport<SqlServer>(() => @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;");
                    break;

                case "activemq":
                    config.UseTransport<ActiveMQ>(() => "ServerUrl=activemq:tcp://localhost:61616?nms.prefetchPolicy.all=100");
                    break;
                case "rabbitmq":
                    config.UseTransport<RabbitMQ>(() => "host=localhost");
                    break;

                default:
                    throw new InvalidOperationException("Illegal transport " + args[2]);
            }

            using (var startableBus = config.InMemoryFaultManagement().UnicastBus().CreateBus())
            {
                Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();

                var processorTimeBefore = Process.GetCurrentProcess().TotalProcessorTime;
                var sendTimeNoTx = SeedInputQueue(numberOfMessages, endpointName, numberOfThreads, false, twoPhaseCommit, saga, true, writeToRaven);
                var sendTimeWithTx = SeedInputQueue(numberOfMessages, endpointName, numberOfThreads, true, twoPhaseCommit, saga, false, writeToRaven);

                Console.WriteLine("Queue seeded");
                Console.ReadLine();
     
                var startTime = DateTime.Now;

                startableBus.Start();

                while (Interlocked.Read(ref Timings.NumberOfMessages) < numberOfMessages * 2)
                    Thread.Sleep(1000);

                var durationSeconds = (Timings.Last - Timings.First.Value).TotalSeconds;
                Console.Out.WriteLine("Threads: {0}, NumMessages: {1}, Serialization: {2}, Transport: {3}, Throughput: {4:0.0} msg/s, Sending: {5:0.0} msg/s, Sending in Tx: {9:0.0} msg/s, TimeToFirstMessage: {6:0.0}s, TotalProcessorTime: {7:0.0}s, Mode:{8}", 
                                      numberOfThreads, 
                                      numberOfMessages * 2, 
                                      args[2], 
                                      args[3], 
                                      Convert.ToDouble(numberOfMessages * 2) / durationSeconds, 
                                      Convert.ToDouble(numberOfMessages) / sendTimeNoTx.TotalSeconds,
                                      (Timings.First - startTime).Value.TotalSeconds,
                                      (Process.GetCurrentProcess().TotalProcessorTime - processorTimeBefore).TotalSeconds,
                                      args[4],
                                      Convert.ToDouble(numberOfMessages) / sendTimeWithTx.TotalSeconds);

            }
        }

        static TimeSpan SeedInputQueue(int numberOfMessages, string inputQueue, int numberOfThreads, bool createTransaction, bool twoPhaseCommit, bool saga, bool startSaga, bool writeToRaven)
        {
            var sw = new Stopwatch();
            var bus = Configure.Instance.Builder.Build<IBus>();

            sw.Start();
            Parallel.For(
                0,
                numberOfMessages,
                new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads },
                x =>
                    {
                        var message = CreateMessage(saga, startSaga, writeToRaven);
                        message.TwoPhaseCommit = twoPhaseCommit;
                        message.Id = x;

                        if (createTransaction)
                        {
                            using (var tx = new TransactionScope())
                            {
                                bus.Send(inputQueue, message);
                                tx.Complete();
                            }
                        }
                        else
                        {
                            bus.Send(inputQueue, message);
                        }
                    });
            sw.Stop();

            return sw.Elapsed;
        }

        private static MessageBase CreateMessage(bool saga, bool startSaga, bool writeToRaven)
        {
            MessageBase message = null;
            if (saga)
            {
                if (startSaga)
                {
                    message = new StartSagaMessage();
                }
                else
                {
                    message = new CompleteSagaMessage();
                }
            }
            else
            {
                if (!writeToRaven) message = new TestMessage();
                else message = new RavenMessage();
            }

            return message;
        }
    }

    internal class TestRavenUnitOfWork : IManageUnitsOfWork
    {
        private readonly IDocumentSession session;

        public TestRavenUnitOfWork(IDocumentSession session)
        {
            this.session = session;
        }

        public void Begin()
        {
        }

        public void End(Exception ex = null)
        {
            if (ex == null)
            {
                session.SaveChanges();
            }
        }
    }

    class TransportConfigOverride : IProvideConfiguration<TransportConfig>
    {
        public static int MaximumConcurrencyLevel;
        public TransportConfig GetConfiguration()
        {
            return new TransportConfig
                {
                    MaximumConcurrencyLevel = MaximumConcurrencyLevel,
                    MaxRetries = 5,
                    MaximumMessageThroughputPerSecond = 0
                };
        }
    }

    class Timings
    {
        public static DateTime? First;
        public static DateTime Last;
        public static Int64 NumberOfMessages;
    }

    class TestMessageHandler:IHandleMessages<TestMessage>
    {
        private static TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();

        public void Handle(TestMessage message)
        {
            if (!Timings.First.HasValue)
            {
                Timings.First = DateTime.Now;
            }
            Interlocked.Increment(ref Timings.NumberOfMessages);

            if (message.TwoPhaseCommit)
            {
                Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            }

            Timings.Last = DateTime.Now;
        }
    }

    class RavenMessageHandler : IHandleMessages<RavenMessage>
    {
        private readonly IDocumentSession session;

        public RavenMessageHandler(IDocumentSession session)
        {
            this.session = session;
        }

        public void Handle(RavenMessage message)
        {
            if (!Timings.First.HasValue)
            {
                Timings.First = DateTime.Now;
            }
            Interlocked.Increment(ref Timings.NumberOfMessages);

            session.Store(new TestObject { Id = Guid.NewGuid() });

            Timings.Last = DateTime.Now;
        }
    }

    public class TestObject
    {
        public Guid Id { get; set; }
    }

    public class SagaData : ISagaEntity
    {
        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual Guid Id { get; set; }

        [Unique]
        public virtual int Number { get; set; }
    }

    class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>
    {
        private static TwoPhaseCommitEnlistment enlistment = new TwoPhaseCommitEnlistment();
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }

        public void Handle(StartSagaMessage message)
        {
            this.BeforeMessage(message);
            this.Data.Number = message.Id;
            this.AfterMessage();
        }

        public void Handle(CompleteSagaMessage message)
        {
            this.BeforeMessage(message);
            this.MarkAsComplete();
            this.AfterMessage();
        }

        private void AfterMessage()
        {
            Timings.Last = DateTime.Now;
        }

        private void BeforeMessage(MessageBase message)
        {
            if (!Timings.First.HasValue)
            {
                Timings.First = DateTime.Now;
            }
            Interlocked.Increment(ref Timings.NumberOfMessages);

            if (message.TwoPhaseCommit)
            {
                Transaction.Current.EnlistDurable(Guid.NewGuid(), enlistment, EnlistmentOptions.None);
            }
        }
    }

    [Serializable]
    public class MessageBase : IMessage
    {
        public int Id { get; set; }
        public bool TwoPhaseCommit { get; set; }
    }

    [Serializable]
    public class TestMessage : MessageBase
    {
    }

    [Serializable]
    public class RavenMessage : MessageBase
    {
    }

    [Serializable]
    public class StartSagaMessage : MessageBase
    {
    }

    [Serializable]
    public class CompleteSagaMessage : MessageBase
    {
    }

    internal class TwoPhaseCommitEnlistment : ISinglePhaseNotification
    {
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }
        
        public void Commit(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            enlistment.Done();
        }
        
        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            singlePhaseEnlistment.Committed();
        }
    }
}
