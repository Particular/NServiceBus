namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    /// <summary>
    /// Defines an initializable storage for subscriptions.
    /// </summary>
    public interface IInitializableSubscriptionStorage : ISubscriptionStorage
    {
        /// <summary>
        /// Notifies the subscription storage that now is the time to perform
        /// any initialization work.
        /// </summary>
        void Init();
    }
}