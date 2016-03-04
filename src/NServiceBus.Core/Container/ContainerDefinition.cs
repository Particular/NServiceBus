namespace NServiceBus.Container
{
    using ObjectBuilder.Common;
    using Settings;

    /// <summary>
    /// Base class for container definitions.
    /// </summary>
    public abstract class ContainerDefinition
    {
        /// <summary>
        /// Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public abstract IContainer CreateContainer(ReadOnlySettings settings);
    }
}