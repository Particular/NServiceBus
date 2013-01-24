namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using NServiceBus.Serializers.Binary;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Serializers.XML;
    using Support;

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