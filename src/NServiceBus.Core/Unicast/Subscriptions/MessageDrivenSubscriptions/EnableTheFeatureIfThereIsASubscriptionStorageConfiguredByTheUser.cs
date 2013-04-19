namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using Features;
    using Logging;

    /// <summary>
    /// This class handles backwards compatibility. If there is a ISubscription storage registered by the user we should use
    /// the message drive´n subscription manager
    /// </summary>
    public class EnableTheFeatureIfThereIsASubscriptionStorageConfiguredByTheUser:IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
            {
                Feature.Enable<MessageDrivenSubscriptions>();
                Logger.InfoFormat("ISubscriptionStorage found in the container. The message driven subscriptions will be available");
            }
                
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(MessageDrivenSubscriptions));
    }
}