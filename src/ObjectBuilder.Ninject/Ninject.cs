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
            IKernel existingKernel;

            if (settings.TryGet("ExistingKernel", out existingKernel))
            {
                return new NinjectObjectBuilder(existingKernel);

            }

            return new NinjectObjectBuilder();
        }
    }
}