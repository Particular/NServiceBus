namespace NServiceBus.AcceptanceTests.Transactions
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class Receive_performance : NServiceBusIntegrationTest
    {
        static int NumberOfTestMessages = 10000;

        [Test]
        public void With_dtc_enabled()
        {
            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(SendTestMessages)
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(s=>DisplayTestResults(s,"DTC"))
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_dtc_suppressed()
        {

            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(b =>
                        {
                            b.CustomConfig(c => Configure.Transactions.Advanced(a => a.SuppressDistributedTransactions = true));
                            SendTestMessages(b);
                        })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(s => DisplayTestResults(s, "No DTC"))
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_no_transactions()
        {

            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Disable());
                        SendTestMessages(b);
                    })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(s => DisplayTestResults(s, "No TX"))
                    .MaxTestParallelism(1)
                    .Run();
        }

        [Test]
        public void With_ambient_tx_suppressed()
        {

            Scenario.Define(() => new Context { NumberOfTestMessages = NumberOfTestMessages })
                    .WithEndpoint<ReceiveEndpoint>(b =>
                    {
                        b.CustomConfig(c => Configure.Transactions.Advanced(a => a.DoNotWrapHandlersExecutionInATransactionScope = true));
                        SendTestMessages(b);
                    })
                    .Done(c => c.Complete)
                    .Repeat(r => r.For(Transports.SqlServer))
                    .Report(s => DisplayTestResults(s, "No Ambient TX"))
                    .MaxTestParallelism(1)
                    .Run();
        }


        public class Context : ScenarioContext
        {
            public int NumberOfTestMessages;

            public DateTime FirstMessageProcessedAt { get; set; }

            public DateTime LastMessageProcessedAt { get; set; }

            public bool Complete { get; set; }
        }

        public class ReceiveEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 10);
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                static int numberOfMessagesProcessed;


                public void Handle(MyMessage messageThatIsEnlisted)
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

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        static void DisplayTestResults(RunSummary summary,string testCase)
        {
            var c = summary.RunDescriptor.ScenarioContext as Context;

            var messagesPerSecondsProcessed = c.NumberOfTestMessages/
                                              (c.LastMessageProcessedAt - c.FirstMessageProcessedAt).TotalSeconds;

            Console.Out.WriteLine("Results: {0} messages, {1} msg/s", c.NumberOfTestMessages,messagesPerSecondsProcessed);

            using (var file = new StreamWriter(".\\PerformanceTestResults.txt", true))
            {
                file.WriteLine(string.Join(";", summary.RunDescriptor.Key + "-" + testCase, c.NumberOfTestMessages, messagesPerSecondsProcessed));
            }
        }

        static void SendTestMessages(EndpointBehaviorBuilder<Context> b)
        {
            b.Given(
                (bus, context) =>
                { Parallel.For(0, context.NumberOfTestMessages, (s, c) => { bus.SendLocal(new MyMessage()); }); });
        }

    }
}