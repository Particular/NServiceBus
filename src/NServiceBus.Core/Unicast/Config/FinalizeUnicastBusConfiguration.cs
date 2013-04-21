namespace NServiceBus.Unicast.Config
{
    using NServiceBus.Config;
    using Settings;
    using Subscriptions;

    class FinalizeUnicastBusConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (SettingsHolder.GetOrDefault<bool>("UnicastBus.AutoSubscribe"))
                InfrastructureServices.Enable<IAutoSubscriptionStrategy>();
        }
    }
}