namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using Serializers.Binary;
    using Serializers.Json;
    using Serializers.XML;
    using Support;

    public class AllSerializers : ScenarioDescriptor
    {
        public AllSerializers()
        {
            Add(new RunDescriptor
                    {
                        Key = "Xml",
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
                Key = "Json",
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
                Key = "Bson",
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
                Key = "Binary",
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