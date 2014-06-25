namespace NServiceBus
{
    using Container;
    using global::StructureMap;
    using ObjectBuilder.StructureMap;
    using Settings;

    /// <summary>
    /// StructureMap Container
    /// </summary>
    public class StructureMap : ContainerDefinition
    {
        /// <summary>
        ///     Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override ObjectBuilder.Common.IContainer CreateContainer(ReadOnlySettings settings)
        {
            IContainer existingContainer;

            if (settings.TryGet("ExistingContainer", out existingContainer))
            {
                return new StructureMapObjectBuilder(existingContainer);

            }

            return new StructureMapObjectBuilder();
        }
    }
}