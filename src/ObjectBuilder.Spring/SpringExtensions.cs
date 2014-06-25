namespace NServiceBus
{
    using System;
    using Container;
    using global::Spring.Context.Support;

    /// <summary>
    /// Spring extension to pass an existing Spring container instance.
    /// </summary>
    public static class SpringExtensions
    {
        /// <summary>
        /// Use the Spring passing in a pre-configured container to be used by NServiceBus.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="container">The existing container instance.</param>
        [CLSCompliant(false)]
        public static void ExistingContainer(this ContainerCustomizations customizations, GenericApplicationContext container)
        {
            customizations.Settings.Set("ExistingContainer", container);
        }
    }
}