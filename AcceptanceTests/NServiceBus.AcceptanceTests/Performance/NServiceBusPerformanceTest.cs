namespace NServiceBus.AcceptanceTests.Transactions
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Performance;

    public class NServiceBusPerformanceTest:NServiceBusAcceptanceTest
    {
        protected static void DisplayTestResults(RunSummary summary, string testCase)
        {
            var c = summary.RunDescriptor.ScenarioContext as PerformanceTestContext;

            var messagesPerSecondsProcessed = c.NumberOfTestMessages /
                                              (c.LastMessageProcessedAt - c.FirstMessageProcessedAt).TotalSeconds;

            Console.Out.WriteLine("Results: {0} messages, {1} msg/s", c.NumberOfTestMessages, messagesPerSecondsProcessed);

            using (var file = new StreamWriter(".\\PerformanceTestResults.txt", true))
            {
                file.WriteLine(string.Join(";", summary.RunDescriptor.Key + "-" + testCase, c.NumberOfTestMessages, messagesPerSecondsProcessed));
            }
        }


    }

    public class PerformanceTestContext:ScenarioContext
    {
        public int NumberOfTestMessages;

        public DateTime FirstMessageProcessedAt { get; set; }

        public DateTime LastMessageProcessedAt { get; set; }
    }
}