namespace NServiceBus
{
    /// <summary>
    /// The signature of a subscription authorizer used by <see cref="MessageDrivenSubscriptionsConfigExtensions.SubscriptionAuthorizer"/>.
    /// </summary>
    /// <param name="context">The current <see cref="IncomingPhysicalMessageContext"/> of the subscription control message.</param>
    /// <returns><code>true</code> to allow the subscription to be stored; <code>false</code> to decline the subscription.</returns>
    public delegate bool SubscriptionAuthorizer(IncomingPhysicalMessageContext context);
}