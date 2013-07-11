namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using AcceptanceTesting;

    public static class BusExtensions
    {
        public static void EnsureSubscriptionMessagesHaveArrived(this IBus bus)
        {
            Configure.Instance.Builder.Build<EndpointConfigurationBuilder.SubscriptionsSpy>()
                     .Wait();
        }
    }
}