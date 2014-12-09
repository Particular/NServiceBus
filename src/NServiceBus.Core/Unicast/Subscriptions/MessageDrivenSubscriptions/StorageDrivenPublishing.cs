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
            context.Container.ConfigureComponent<Unicast.Publishing.StorageDrivenPublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.Translator, context.Settings.Get("Routing.Translator"));
        }
    }
}