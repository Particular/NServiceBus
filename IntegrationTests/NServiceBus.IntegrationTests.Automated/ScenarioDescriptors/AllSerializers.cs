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


            Add(Serializers.Bson);


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

    public class Serializers
    {
        public static RunDescriptor Bson = new RunDescriptor
            {
                Key = "Bson",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (BsonMessageSerializer).AssemblyQualifiedName
                            }
                        }
            };
    }
}