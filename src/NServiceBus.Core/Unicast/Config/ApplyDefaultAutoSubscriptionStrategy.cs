namespace NServiceBus.Unicast.Config
{
    using Subscriptions;

    /// <summary>
    /// Defaults the auto subscription strategy
    /// </summary>
    public class ApplyDefaultAutoSubscriptionStrategy:IWantToRunBeforeConfigurationIsFinalized
    {
        public static bool DoNotAutoSubscribeSagas { get; set; }
        public static bool AllowSubscribeToSelf { get; set; }

        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<IAutoSubscriptionStrategy>())
                return;

            Configure.Instance.Configurer.ConfigureComponent<DefaultAutoSubscriptionStrategy>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p=>p.DoNotAutoSubscribeSagas,DoNotAutoSubscribeSagas)
                .ConfigureProperty(p => p.AllowSubscribeToSelf, AllowSubscribeToSelf);
        }
    }
}