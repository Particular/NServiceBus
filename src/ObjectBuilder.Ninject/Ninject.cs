namespace NServiceBus
{
    using Container;
    using global::Ninject;
    using ObjectBuilder.Ninject;
    using Settings;

    /// <summary>
    /// Ninject Container
    /// </summary>
    public class Ninject : ContainerDefinition
    {
        /// <summary>
        ///     Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override ObjectBuilder.Common.IContainer CreateContainer(ReadOnlySettings settings)
        {
            IKernel existingContainer;

            if (settings.TryGet("ExistingContainer", out existingContainer))
            {
                return new NinjectObjectBuilder(existingContainer);

            }

            return new NinjectObjectBuilder();
        }
    }
}