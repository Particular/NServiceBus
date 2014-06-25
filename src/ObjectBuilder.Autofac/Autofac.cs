namespace NServiceBus
{
    using global::Autofac;
    using ObjectBuilder.Autofac;
    using Settings;

    /// <summary>
    /// Autofac Container
    /// </summary>
    public class Autofac : ContainerDefinition
    {
        /// <summary>
        ///     Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override ObjectBuilder.Common.IContainer CreateContainer(ReadOnlySettings settings)
        {
            ILifetimeScope existingContainer;

            if (settings.TryGet("ExistingContainer", out existingContainer))
            {
                return new AutofacObjectBuilder(existingContainer);

            }

            return new AutofacObjectBuilder();
        }
    }
}