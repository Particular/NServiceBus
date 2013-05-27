using System;

namespace Runner
{
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    using NServiceBus;
    using NServiceBus.Features;
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

            if (saga)
            {
                Configure.Features.Enable<Sagas>();

                if (nhibernate)
                {
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
                var sendTimeNoTx = SeedInputQueue(numberOfMessages, endpointName, numberOfThreads, false, twoPhaseCommit, saga, true);
                var sendTimeWithTx = SeedInputQueue(numberOfMessages, endpointName, numberOfThreads, true, twoPhaseCommit, saga, false);

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
    }
}
