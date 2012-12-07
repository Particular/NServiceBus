using System;

namespace Runner
{
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

            TransportConfigOverride.MaxDegreeOfParallelism = numberOfThreads;

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
                    throw new InvalidOperationException("Illegal seralization format " + args[2]);
            }
    
            
            var startableBus = config
                     .MsmqTransport()
                     .InMemoryFaultManagement()
                     .UnicastBus()
                     .CreateBus();

            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();
            
            var bus = Configure.Instance.Builder.Build<IBus>();

            var message = new TestMessage();
            Parallel.For(0, numberOfMessages, x => bus.Send("PerformanceTest", message));
            
            startableBus.Start();
            
            while(Interlocked.Read(ref TestMessageHandler.NumberOfMessages) < numberOfMessages)
                Thread.Sleep(1000);

            var durationSeconds = (TestMessageHandler.Last - TestMessageHandler.First.Value).TotalSeconds;
            Console.Out.WriteLine("Threads: {0}, NumMessages: {1}, Serialization: {2}, Throughput: {3:0.0} msg/s", numberOfThreads, numberOfMessages, args[2], Convert.ToDouble(numberOfMessages) / durationSeconds);

            Environment.Exit(0);
        }
    }

    class TransportConfigOverride : IProvideConfiguration<TransportConfig>
    {
        public static int MaxDegreeOfParallelism;
        public TransportConfig GetConfiguration()
        {
            return new TransportConfig
                {
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    MaxRetries = 5,
                    MaxMessageThroughputPerSecond = 0

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
