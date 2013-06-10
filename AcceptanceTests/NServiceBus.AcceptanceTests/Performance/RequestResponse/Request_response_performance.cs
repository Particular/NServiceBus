namespace NServiceBus.AcceptanceTests.Performance.RequestResponse
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting.Support;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class Request_response_performance : NServiceBusPerformanceTest
    {
        [Test]
        public void With_dtc_enabled()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ClientEndpoint>(b => SendMessages(b))
                    .WithEndpoint<ServerEndpoint>()
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Default))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_dtc_supressed()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ClientEndpoint>(b =>
                        {
                            b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DisableDistributedTransactions()));
                            SendMessages(b);
                        })
                    .WithEndpoint<ServerEndpoint>(b => b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DisableDistributedTransactions())))
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Default))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_no_transactions()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ClientEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Disable());
                        SendMessages(b);
                    })
                    .WithEndpoint<ServerEndpoint>(b => b.CustomConfig(c => Configure.Transactions.Disable()))
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.Default))
                    .Report(DisplayTestResults)
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_ambient_tx_suppressed()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                .WithEndpoint<ClientEndpoint>(b =>
                {
                    b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DoNotWrapHandlersExecutionInATransactionScope()));
                    SendMessages(b);
                })
                .WithEndpoint<ServerEndpoint>(b => b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DoNotWrapHandlersExecutionInATransactionScope())))
                .Done(c => c.Complete)
                .Repeat(r => r.For(Transports.Default))
                .Report(DisplayTestResults)
                .MaxTestParallelism(1)
                .Run();
        }

        private static EndpointBehaviorBuilder<Context> SendMessages(EndpointBehaviorBuilder<Context> b)
        {
            return b.Given((bus, context) => Parallel.For(0, context.NumberOfTestMessages, (s, c) => bus.Send(new MyMessage())));
        }

        public class Context : PerformanceTestContext
        {
            public bool Complete { get; set; }
        }

        public class ClientEndpoint : EndpointConfigurationBuilder
        {
            public ClientEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = MaxConcurrencyLevel)
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
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = MaxConcurrencyLevel);
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