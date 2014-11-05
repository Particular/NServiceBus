namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using NServiceBus.AcceptanceTesting.Support;

    public class AllTransactionSettings : ScenarioDescriptor
    {
        public AllTransactionSettings()
        {
            Add(TransactionSettings.DistributedTransaction);
            Add(TransactionSettings.LocalTransaction);
            Add(TransactionSettings.NoTransaction);
        }
    }
}