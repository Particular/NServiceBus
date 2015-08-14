namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Provides details about the current subscription storage operation.
    /// </summary>
    public class SubscriptionStorageOptions
    {
        /// <summary>
        /// Creates a new instance of the SubscriptionStorageOptions class.
        /// </summary>
        /// <param name="context">The context.</param>
        public SubscriptionStorageOptions(ContextBag context)
        {
            Context = context;
        }

        /// <summary>
        /// Access to the behavior context.
        /// </summary>
        public ReadOnlyContextBag Context { get; private set; }
    }
}