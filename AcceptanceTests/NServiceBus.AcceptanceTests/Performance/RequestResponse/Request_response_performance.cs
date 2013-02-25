namespace NServiceBus.AcceptanceTests.Performance.RequestResponse
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Transactions;

    public class Request_response_performance : NServiceBusPerformanceTest
    {
        static int NumberOfTestMessages = 1000;
        static int ConcurrencyLevel = 30;

        [Test]
        public void With_dtc_enabled()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ClientEndpoint>(b => b.Given((bus, context) => Parallel.For(0, context.NumberOfTestMessages, (s, c) => bus.Send(new MyMessage()))))
                    .WithEndpoint<ServerEndpoint>()
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(s => DisplayTestResults(s, "DTC"))
                    .MaxTestParallelism(1)
                    .Run();
        }


        public class Context : PerformanceTestContext
        {
            public bool Complete { get; set; }
        }

        public class ClientEndpoint : EndpointConfigurationBuilder
        {
            public ClientEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.PurgeOnStartup(true))
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = ConcurrencyLevel)
                    .AddMapping<MyMessage>(typeof(ServerEndpoint));
            }

            public class MyMessageHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                static int numberOfMessagesProcessed;


                public void Handle(MyResponse messageThatIsEnlisted)
                {
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
            }
        }

        public class ServerEndpoint : EndpointConfigurationBuilder
        {
            public ServerEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.PurgeOnStartup(true))
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = ConcurrencyLevel);
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Bus.Reply(new MyResponse());
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        [Serializable]
        public class MyResponse : IMessage
        {
        }
    }
}