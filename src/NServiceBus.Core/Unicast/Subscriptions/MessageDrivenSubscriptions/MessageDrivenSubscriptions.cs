namespace NServiceBus.Features
{
    using NServiceBus.Transports;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Used to configure Message Driven Subscriptions
    /// </summary>
    public class MessageDrivenSubscriptions : Feature
    {

        internal MessageDrivenSubscriptions()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(builder=>new SubscriptionManager(builder.Build<Configure>().PublicReturnAddress.ToString(),
                builder.Build<ISendMessages>()), DependencyLifecycle.SingleInstance);
        }
    }
}