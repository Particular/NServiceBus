namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using Serializers.Binary;
    using Serializers.Json;
    using Support;
    using Serializers.XML;

    public class AllSerializers : ScenarioDescriptor
    {
        public AllSerializers()
        {
            Add(new RunDescriptor
                    {
                        Name = "Xml serialization",
                        Settings =
                            new Dictionary<string, string>
                                {
                                    {
                                        "Serializer",typeof(XmlMessageSerializer).AssemblyQualifiedName
                                    }
                                }
                    });

            Add(new RunDescriptor
            {
                Name = "Json serialization",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Serializer",typeof(JsonMessageSerializer).AssemblyQualifiedName
                                    }
                                }
            });


            Add(new RunDescriptor
            {
                Name = "Bson serialization",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Serializer",typeof(BsonMessageSerializer).AssemblyQualifiedName
                                    }
                                }
            });


            Add(new RunDescriptor
            {
                Name = "Binary serialization",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Serializer",typeof(MessageSerializer).AssemblyQualifiedName
                                    }
                                }
            });
        }
    }
}