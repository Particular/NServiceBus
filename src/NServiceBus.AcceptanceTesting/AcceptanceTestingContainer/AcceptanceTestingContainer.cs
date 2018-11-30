namespace NServiceBus
{
    using Container;
    using ObjectBuilder.Common;
    using Settings;

    public class AcceptanceTestingContainer : ContainerDefinition
    {
        public override IContainer CreateContainer(ReadOnlySettings settings)
        {
            return new AcceptanceTestingBuilder();
        }
    }
}