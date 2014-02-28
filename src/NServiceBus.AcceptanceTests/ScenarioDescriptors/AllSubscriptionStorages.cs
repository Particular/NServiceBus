namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using AcceptanceTesting.Support;

    public class AllSubscriptionStorages : ScenarioDescriptor
    {
        public AllSubscriptionStorages()
        {
            Add(SubscriptionStorages.InMemory);
            Add(SubscriptionStorages.Raven);
            Add(SubscriptionStorages.Msmq);
        }
    }
}