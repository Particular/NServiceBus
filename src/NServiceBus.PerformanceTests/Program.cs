using System;

namespace Runner
{
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Persistence.NHibernate;
    using Runner.Saga;

    class Program
    {
        static void Main(string[] args)
        {
            var numberOfThreads = int.Parse(args[0]);
            bool volatileMode = (args[4].ToLower() == "volatile");
            bool suppressDTC = (args[4].ToLower() == "suppressdtc");
            bool twoPhaseCommit = (args[4].ToLower() == "twophasecommit");
            bool saga = (args[5].ToLower() == "sagamessages");
            bool nhibernate = (args[6].ToLower() == "nhibernate");
            int concurrency = int.Parse(args[7]);

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

            switch (args[2].ToLower())
            {
                case "xml":
                    Configure.Serialization.Xml();
                    break;

                case "json":
                    Configure.Serialization.Json();
                    break;

                case "bson":
                    Configure.Serialization.Bson();
                    break;

                case "bin":
                    Configure.Serialization.Binary();
                    break;

                default:
                    throw new InvalidOperationException("Illegal serialization format " + args[2]);
            }

            Configure.Features.Disable<Audit>();

            //Configure.Instance.UnicastBus().IsolationLevel(IsolationLevel.Snapshot);
            //Console.Out.WriteLine("Snapshot");

            if (saga)
            {
                Configure.Features.Enable<Sagas>();

                if (nhibernate)
                {
                    NHibernateSettingRetriever.ConnectionStrings = () =>
                    {
                        var c = new ConnectionStringSettingsCollection();

                        c.Add(new ConnectionStringSettings("NServiceBus/Persistence", SqlServerConnectionString));
                        return c;

                    };
                    config.UseNHibernateSagaPersister();

                }
                else
                {
                    config.RavenSagaPersister();
                }
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

                //todo: dynamically load the transports or autodetect like we do in the acceptance tests
                //case "sqlserver":
                //    config.UseTransport<SqlServer>( () => SqlServerConnectionString);
                //    break;

                //case "activemq":
                //    config.UseTransport<ActiveMQ>(() => "ServerUrl=activemq:tcp://localhost:61616?nms.prefetchPolicy.all=100");
                //    break;
                //case "rabbitmq":
                //    config.UseTransport<RabbitMQ>(() => "host=localhost");
                //    break;

                default:
                    throw new InvalidOperationException("Illegal transport " + args[2]);
            }

            using (var startableBus = config.InMemoryFaultManagement().UnicastBus().CreateBus())
            {
                Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();

                if (saga)
                {
                    SeedSagaMessages(numberOfMessages, endpointName, concurrency);
                }
                else
                {
                    Statistics.SendTimeNoTx = SeedInputQueue(numberOfMessages / 2, endpointName, numberOfThreads, false, twoPhaseCommit, saga, true);
                    Statistics.SendTimeWithTx = SeedInputQueue(numberOfMessages / 2, endpointName, numberOfThreads, true, twoPhaseCommit, saga, true);
                }

                Statistics.StartTime = DateTime.Now;

                startableBus.Start();

                while (Interlocked.Read(ref Statistics.NumberOfMessages) < numberOfMessages)
                    Thread.Sleep(1000);


                DumpSetting(args);
                Statistics.Dump();



            }
        }

        static void DumpSetting(string[] args)
        {
            Console.Out.WriteLine("---------------- Settings ----------------");
            Console.Out.WriteLine("Threads: {0}, Serialization: {1}, Transport: {2}",
                                  args[0],
                                  args[2],
                                  args[3]);
        }

        static void SeedSagaMessages(int numberOfMessages, string inputQueue, int concurrency)
        {
            var bus = Configure.Instance.Builder.Build<IBus>();

            for (int i = 0; i < numberOfMessages / concurrency; i++)
            {

                for (int j = 0; j < concurrency; j++)
                {
                    bus.Send(inputQueue, new StartSagaMessage
                    {
                        Id = i
                    });
                }
            }

        }

        static TimeSpan SeedInputQueue(int numberOfMessages, string inputQueue, int numberOfThreads, bool createTransaction, bool twoPhaseCommit, bool saga, bool startSaga)
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
                    var message = CreateMessage(saga, startSaga);
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

        private static MessageBase CreateMessage(bool saga, bool startSaga)
        {
            if (saga)
            {
                if (startSaga)
                {
                    return new StartSagaMessage();
                }

                return new CompleteSagaMessage();
            }

            return new TestMessage();
        }

        static string SqlServerConnectionString = @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;";


    }
}
