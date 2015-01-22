namespace NServiceBus.Features
{
    /// <summary>
    /// Adds support for pub/sub using a external subscription storage. This brings pub/sub to transport that lacks native support.
    /// </summary>
    public class StorageDrivenPublishing : Feature
    {
        internal StorageDrivenPublishing()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.Get<bool>("Endpoint.SendOnly"))
            {
                context.Container.ConfigureComponent<Unicast.Publishing.StorageDrivenPublisherNonFunctionalPublisher>(DependencyLifecycle.InstancePerCall); 
            }
            else
            {
                context.Container.ConfigureComponent<Unicast.Publishing.StorageDrivenPublisher>(DependencyLifecycle.InstancePerCall);                
            }
        }
    }
}