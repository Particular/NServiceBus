namespace NServiceBus
{
    using global::Autofac;

    /// <summary>
    /// Autofac extension to pass an existing Autofac container instance.
    /// </summary>
    public static class AutofacExtensions
    {
        /// <summary>
        /// Use the Autofac passing in a pre-configured container to be used by NServiceBus.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="container">The existing container instance.</param>
        public static void ExistingContainer(this ContainerCustomizations customizations, ILifetimeScope container)
        {
            customizations.Settings.Set("ExistingContainer", container);
        }
    }
}