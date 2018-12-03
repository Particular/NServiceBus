namespace NServiceBus
{
    using Container;
    using ObjectBuilder.Common;
    using Settings;

    /// <summary>
    /// Container that enforces registration immutability once the first instance has been resolved.
    /// </summary>
    public class AcceptanceTestingContainer : ContainerDefinition
    {
        public override IContainer CreateContainer(ReadOnlySettings settings)
        {
            return new AcceptanceTestingBuilder();
        }
    }
}
