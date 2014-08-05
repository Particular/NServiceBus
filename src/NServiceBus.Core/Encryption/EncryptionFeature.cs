namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Encryption;
    using NServiceBus.ObjectBuilder;

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
            Func<IBuilder, IEncryptionService> func;
            if (context.Settings.GetEncryptionServiceConstructor(out func))
            {
                context.Container.ConfigureComponent(func, DependencyLifecycle.SingleInstance);
                context.Container.ConfigureComponent<EncryptionMutator>(DependencyLifecycle.InstancePerCall);

                context.Pipeline.Register<EncryptBehavior.EncryptRegistration>();
                context.Pipeline.Register<DecryptBehavior.DecryptRegistration>();
            }
        }
    }
}

