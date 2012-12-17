using System;

namespace Runner
{
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Config.ConfigurationSource;

    class Program
    {
        static void Main(string[] args)
        {
            var numberOfThreads = int.Parse(args[0]);

            TransportConfigOverride.MaximumConcurrencyLevel = numberOfThreads;

            var numberOfMessages = int.Parse(args[1]);


            var config = Configure.With()
                .DefineEndpointName("PerformanceTest")
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

            switch (args[3].ToLower())
            {
                case "msmq":
                    config.MsmqTransport();
                    break;

                case "sqlserver":
                    config.SqlServerTransport(@"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;");
                    break;

                case "activemq":
                    config.ActiveMqTransport("activemq:tcp://localhost:61616");
                    break;
                case "rabbitmq":
                    config.RabbitMqTransport("host=localhost");
                    break;

                default:
                    throw new InvalidOperationException("Illegal transport " + args[2]);
            }
            
            var startableBus = config
                     .InMemoryFaultManagement()
                     .UnicastBus()
                     .CreateBus();

            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();
            
            var sendTime = SeedInputQueue(numberOfMessages);

            var startTime = DateTime.Now;

            startableBus.Start();
            
            while(Interlocked.Read(ref TestMessageHandler.NumberOfMessages) < numberOfMessages)
                Thread.Sleep(1000);

            var durationSeconds = (TestMessageHandler.Last - TestMessageHandler.First.Value).TotalSeconds;
            Console.Out.WriteLine("Threads: {0}, NumMessages: {1}, Serialization: {2}, Transport: {3}, Throughput: {4:0.0} msg/s, Sending: {5:0.0} msg/s, TimeToFirstMessage: {6:0.0}s", 
                                numberOfThreads, 
                                numberOfMessages, 
                                args[2], 
                                args[3], 
                                Convert.ToDouble(numberOfMessages) / durationSeconds, 
                                Convert.ToDouble(numberOfMessages) / sendTime.TotalSeconds,
                                (TestMessageHandler.First - startTime).Value.TotalSeconds);

            Environment.Exit(0);
        }

        static TimeSpan SeedInputQueue(int numberOfMessages)
        {
            var sw = new Stopwatch();
            var bus = Configure.Instance.Builder.Build<IBus>();

            var message = new TestMessage();
            sw.Start();
            Parallel.For(0, numberOfMessages, x => bus.Send("PerformanceTest", message));
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
