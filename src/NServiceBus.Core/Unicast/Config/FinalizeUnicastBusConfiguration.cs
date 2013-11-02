namespace NServiceBus.Unicast.Config
{
    using AutomaticSubscriptions;
    using NServiceBus.Config;
    using Settings;

    class FinalizeUnicastBusConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (SettingsHolder.GetOrDefault<bool>("UnicastBus.AutoSubscribe"))
                InfrastructureServices.Enable<IAutoSubscriptionStrategy>();
        }
    }
}