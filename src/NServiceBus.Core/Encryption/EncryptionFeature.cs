namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Encryption;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Used to configure encryption.
    /// </summary>
    public class EncryptionFeature:Feature
    {
        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            Func<IConfigureComponents, ReadOnlySettings, IEncryptionService> func;
            if (context.Settings.GetEncryptionServiceConstructor(out func))
            {
                context.Container.ConfigureComponent(builder => func(context.Container,context.Settings), DependencyLifecycle.SingleInstance);
                context.Container.ConfigureComponent<EncryptionMessageMutator>(DependencyLifecycle.InstancePerCall);
            }
        }
    }
}
