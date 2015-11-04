namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using NServiceBus.Transports;

    /// <summary>
    /// Allows subscribers to register by sending a subscription message to this endpoint.
    /// </summary>
    public class MessageDrivenSubscriptions : Feature
    {

        internal MessageDrivenSubscriptions()
        {
            EnableByDefault();
            Prerequisite(c => !c.Settings.Get<TransportDefinition>().HasNativePubSubSupport, "The transport supports native pub sub");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();
            var authorizer = context.Settings.GetSubscriptionAuthorizer();
            if (authorizer == null)
            {
                authorizer = _ => true;
            }
            context.Container.RegisterSingleton(authorizer);
        }
    }
}