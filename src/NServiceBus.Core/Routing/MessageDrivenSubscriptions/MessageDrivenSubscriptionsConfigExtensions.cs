namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides extensions for configuring message driven subscriptions.
    /// </summary>
    public static class MessageDrivenSubscriptionsConfigExtensions
    {
        /// <summary>
        /// Set a Authorizer to be used when verifying a <see cref="MessageIntentEnum.Subscribe"/> or <see cref="MessageIntentEnum.Unsubscribe"/> message.
        /// </summary>
        /// <remarks>This is a "single instance" extension point. So calling this api multiple time will result in only the last one added being executed at message receive time.</remarks>
        /// <param name="transportExtensions">The <see cref="TransportExtensions"/> to extend.</param>
        /// <param name="authorizer">The <see cref="Func{TI,TR}"/> to execute.</param>
        public static void SubscriptionAuthorizer(this TransportExtensions transportExtensions, Func<IIncomingPhysicalMessageContext, bool> authorizer)
        {
            Guard.AgainstNull(nameof(authorizer), authorizer);
            var settings = transportExtensions.Settings;

            settings.Set("SubscriptionAuthorizer", authorizer);
        }

        internal static Func<IIncomingPhysicalMessageContext, bool> GetSubscriptionAuthorizer(this ReadOnlySettings settings)
        {
            Func<IIncomingPhysicalMessageContext, bool> authorizer;
            settings.TryGet("SubscriptionAuthorizer",out authorizer);
            return authorizer;
        }
    }
}