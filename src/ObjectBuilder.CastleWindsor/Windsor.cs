namespace NServiceBus
{
    using Castle.Windsor;
    using ObjectBuilder.CastleWindsor;
    using Settings;

    /// <summary>
    /// Windsor Container
    /// </summary>
    public class Windsor : ContainerDefinition
    {
        /// <summary>
        ///     Implementers need to new up a new container.
        /// </summary>
        /// <param name="settings">The settings to check if an existing container exists.</param>
        /// <returns>The new container wrapper.</returns>
        public override ObjectBuilder.Common.IContainer CreateContainer(ReadOnlySettings settings)
        {
            IWindsorContainer existingContainer;

            if (settings.TryGet("ExistingContainer", out existingContainer))
            {
                return new WindsorObjectBuilder(existingContainer);

            }

            return new WindsorObjectBuilder();
        }
    }
}