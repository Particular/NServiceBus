namespace NServiceBus
{
    using global::Spring.Context.Support;
    using ObjectBuilder.Spring;
    using Settings;

    /// <summary>
    /// Spring Container
    /// </summary>
    public class Spring : ContainerDefinition
    {
        /// <summary>
        ///     Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override ObjectBuilder.Common.IContainer CreateContainer(ReadOnlySettings settings)
        {
            GenericApplicationContext existingContainer;

            if (settings.TryGet("ExistingContainer", out existingContainer))
            {
                return new SpringObjectBuilder(existingContainer);

            }

            return new SpringObjectBuilder();
        }
    }
}