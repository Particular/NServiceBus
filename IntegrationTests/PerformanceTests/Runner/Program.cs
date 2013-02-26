using System;

namespace Runner
{
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    class Program
    {
        static void Main(string[] args)
        {
            var numberOfThreads = int.Parse(args[0]);
            bool volatileMode = (args[4].ToLower() == "volatile");
            bool suppressDTC = (args[4].ToLower() == "suppressdtc");

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

            if (volatileMode)
            {
                Configure.Endpoint.AsVolatile();
            }

            if (suppressDTC)
            {
                Configure.Transactions.Advanced().SuppressDistributedTransactions = true;
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
                var sendTimeNoTx = SeedInputQueue(numberOfMessages,endpointName, numberOfThreads, false);
                var sendTimeWithTx = SeedInputQueue(numberOfMessages, endpointName, numberOfThreads, true);

                var startTime = DateTime.Now;

                startableBus.Start();
            
                while(Interlocked.Read(ref TestMessageHandler.NumberOfMessages) < numberOfMessages * 2)
                    Thread.Sleep(1000);

                var durationSeconds = (TestMessageHandler.Last - TestMessageHandler.First.Value).TotalSeconds;
                Console.Out.WriteLine("Threads: {0}, NumMessages: {1}, Serialization: {2}, Transport: {3}, Throughput: {4:0.0} msg/s, Sending: {5:0.0} msg/s, Sending in Tx: {9:0.0} msg/s, TimeToFirstMessage: {6:0.0}s, TotalProcessorTime: {7:0.0}s, Mode:{8}", 
                                      numberOfThreads, 
                                      numberOfMessages * 2, 
                                      args[2], 
                                      args[3], 
                                      Convert.ToDouble(numberOfMessages * 2) / durationSeconds, 
                                      Convert.ToDouble(numberOfMessages) / sendTimeNoTx.TotalSeconds,
                                      (TestMessageHandler.First - startTime).Value.TotalSeconds,
                                      (Process.GetCurrentProcess().TotalProcessorTime - processorTimeBefore).TotalSeconds,
                                      args[4],
                                      Convert.ToDouble(numberOfMessages) / sendTimeWithTx.TotalSeconds);

            }
        }

        static TimeSpan SeedInputQueue(int numberOfMessages, string inputQueue, int numberOfThreads, bool createTransaction)
        {
            var sw = new Stopwatch();
            var bus = Configure.Instance.Builder.Build<IBus>();
            var message = new TestMessage();

            sw.Start();
            Parallel.For(0, numberOfMessages, new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads },
                x =>
                {
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

    class TestMessageHandler:IHandleMessages<TestMessage>
    {
        public static DateTime? First;
        public static DateTime Last;
        public static Int64 NumberOfMessages;
        

        public void Handle(TestMessage message)
        {
            if (!First.HasValue)
            {
                First = DateTime.Now;
            }
            Interlocked.Increment(ref NumberOfMessages);
                
            Last = DateTime.Now;
        }
    }

    [Serializable]
    public class TestMessage:IMessage
    {
        
    }
}
