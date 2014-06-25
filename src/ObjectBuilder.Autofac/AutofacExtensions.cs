namespace NServiceBus
{
    using global::Autofac;

    /// <summary>
    /// Autofac extension to pass an exsiting Autofac container instance.
    /// </summary>
    public static class AutofacExtensions
    {
        /// <summary>
        /// Use the Autofac passing in a pre-configured container to be used by NServiceBus.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="rootScope">The root-most lifetime scope.</param>
        public static void ExistingContainer(this ContainerCustomizations customizations, ILifetimeScope rootScope)
        {
            customizations.Settings.Set("ExistingContainer", rootScope);
        }
    }
}