using System.Collections.Generic;

namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using NServiceBus.AcceptanceTesting.Support;


    public static class TransactionSettings
    {
        public static readonly RunDescriptor DistributedTransaction = new RunDescriptor
        {
            Key = "DistributedTransaction",
            Settings =
                new Dictionary<string, string>()
        };

        public static readonly RunDescriptor LocalTransaction = new RunDescriptor
        {
            Key = "LocalTransaction",
            Settings =
                new Dictionary<string, string>
                {
                    {"Transactions.SuppressDistributedTransactions", bool.TrueString}
                }
        };

        public static readonly RunDescriptor NoTransaction = new RunDescriptor
        {
            Key = "NoTransaction",
            Settings =
                new Dictionary<string, string>
                {
                    {"Transactions.Disable", bool.TrueString},
                }
        };
    }
}
