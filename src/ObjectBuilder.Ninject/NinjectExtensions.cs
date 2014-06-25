namespace NServiceBus
{
    using global::Ninject;

    /// <summary>
    /// Ninject extension to pass an existing Ninject container instance.
    /// </summary>
    public static class NinjectExtensions
    {
        /// <summary>
        /// Use the Ninject passing in a pre-configured container to be used by NServiceBus.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="container">The existing container instance.</param>
        public static void ExistingContainer(this ContainerCustomizations customizations, IKernel container)
        {
            customizations.Settings.Set("ExistingContainer", container);
        }
    }
}