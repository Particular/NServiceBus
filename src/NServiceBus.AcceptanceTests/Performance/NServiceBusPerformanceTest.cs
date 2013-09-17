namespace NServiceBus.AcceptanceTests.Performance
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;

    public class NServiceBusPerformanceTest:NServiceBusAcceptanceTest
    {
        protected static int NumberOfTestMessages
        {
            get
            {
                int nr;

                if (!int.TryParse(Environment.GetEnvironmentVariable("NServiceBus.MaxMessagesForPerformanceTests"),out nr))
                    return 3000;

                return nr;
            }
        }

        protected static int MaxConcurrencyLevel
        {
            get
            {
                int nr;

                if (!int.TryParse(Environment.GetEnvironmentVariable("NServiceBus.MaxConcurrencyLevel"), out nr))
                    return 15;

                return nr;
            }
        }

        protected static void DisplayTestResults(RunSummary summary)
        {

      
            var caller =new StackTrace().GetFrames().First(f => typeof(NServiceBusPerformanceTest).IsAssignableFrom(f.GetMethod().DeclaringType.BaseType));

            var testCategory = caller.GetMethod().DeclaringType.Namespace.Replace(typeof(NServiceBusPerformanceTest).Namespace + ".", "");
            var testCase = caller.GetMethod().Name;

            var c = summary.RunDescriptor.ScenarioContext as PerformanceTestContext;

            var messagesPerSecondsProcessed = c.NumberOfTestMessages /
                                              (c.LastMessageProcessedAt - c.FirstMessageProcessedAt).TotalSeconds;

            Console.Out.WriteLine("Results: {0} messages, {1} msg/s", c.NumberOfTestMessages, messagesPerSecondsProcessed);

            using (var file = new StreamWriter(".\\PerformanceTestResults.txt", true))
            {
                file.WriteLine(string.Join(";", summary.RunDescriptor.Key, testCategory,testCase, c.NumberOfTestMessages, messagesPerSecondsProcessed));
            }

            Console.Out.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1:0}']", summary.RunDescriptor.Key + "." + testCategory + "." + testCase, messagesPerSecondsProcessed);
        }


    }

    public class PerformanceTestContext:ScenarioContext
    {
        public int NumberOfTestMessages;

        public DateTime FirstMessageProcessedAt { get; set; }

        public DateTime LastMessageProcessedAt { get; set; }
    }
}