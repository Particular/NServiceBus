namespace NServiceBus
{
    using Castle.Windsor;
    using Container;

    /// <summary>
    /// Windsor extension to pass an existing Windsor container instance.
    /// </summary>
    public static class WindsorExtensions
    {
        /// <summary>
        /// Use a pre-configured native Windsor container.
        /// </summary>
        /// <param name="customizations"></param>
        /// <param name="container">The existing container instance.</param>
        public static void ExistingContainer(this ContainerCustomizations customizations, IWindsorContainer container)
        {
            customizations.Settings.Set("ExistingContainer", container);
        }
    }
}