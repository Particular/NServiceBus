namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using NServiceBus.AcceptanceTesting.Support;

    public class AllSerializers : ScenarioDescriptor
    {
        public AllSerializers()
        {
            Add(Serializers.Bson);
            Add(Serializers.Json);
            Add(Serializers.Xml);
            Add(Serializers.Binary);
        }
    }
}