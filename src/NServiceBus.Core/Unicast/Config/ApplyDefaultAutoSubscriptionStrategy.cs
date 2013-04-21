namespace NServiceBus.Unicast.Config
{
    using NServiceBus.Config;
    using Subscriptions;

    /// <summary>
    /// Defaults the auto subscription strategy
    /// </summary>
    public class ApplyDefaultAutoSubscriptionStrategy:IWantToRunBeforeConfigurationIsFinalized
    {
        public static bool DoNotAutoSubscribeSagas { get; set; }
        public static bool AllowSubscribeToSelf { get; set; }
        public static bool SubscribePlainMessages { get; set; }

        public void Run()
        {
            InfrastructureServices.SetDefaultFor<IAutoSubscriptionStrategy>(() => Configure.Component<DefaultAutoSubscriptionStrategy>(DependencyLifecycle.InstancePerCall)
                                                                                           .ConfigureProperty(p => p.DoNotAutoSubscribeSagas, DoNotAutoSubscribeSagas)
                                                                                           .ConfigureProperty(p => p.SubscribePlainMessages, SubscribePlainMessages)
                                                                                           .ConfigureProperty(p => p.AllowSubscribeToSelf, AllowSubscribeToSelf));
        }
    }
}