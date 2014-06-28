namespace NServiceBus
{
    using Container;
    using global::Ninject;

    /// <summary>
    /// Ninject extension to pass an existing Ninject container instance.
    /// </summary>
    public static class NinjectExtensions
    {
        /// <summary>
        /// Use a pre-configured Ninject kernel
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="kernel">The existing container instance.</param>
        public static void ExistingKernel(this ContainerCustomizations customizations, IKernel kernel)
        {
            customizations.Settings.Set("ExistingKernel", kernel);
        }
    }
}