namespace NServiceBus
{
    using Container;
    using global::Autofac;

    /// <summary>
    /// Autofac extension to pass an existing Autofac container instance.
    /// </summary>
    public static class AutofacExtensions
    {
        /// <summary>
        /// Use the a pre-configured AutoFac lifetime scope.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="lifetimeScope">The existing lifetime scope to use.</param>
        public static void ExistingLifetimeScope(this ContainerCustomizations customizations, ILifetimeScope lifetimeScope)
        {
            customizations.Settings.Set("ExistingLifetimeScope", lifetimeScope);
        }
    }
}