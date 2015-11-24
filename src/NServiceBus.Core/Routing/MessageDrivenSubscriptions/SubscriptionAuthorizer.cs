namespace NServiceBus
{
    /// <summary>
    /// The signature of a subscription authorizer used by <see cref="MessageDrivenSubscriptionsConfigExtensions.SubscriptionAuthorizer"/>.
    /// </summary>
    public delegate bool SubscriptionAuthorizer(PhysicalMessageProcessingContext context);
}