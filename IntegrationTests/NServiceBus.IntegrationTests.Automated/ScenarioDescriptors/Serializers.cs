namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using NServiceBus.Serializers.Binary;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Serializers.XML;
    using Support;

    public static class Serializers
    {
        public static readonly RunDescriptor Binary = new RunDescriptor
            {
                Key = "Binary",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (MessageSerializer).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor Bson = new RunDescriptor
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

        public static readonly RunDescriptor Xml = new RunDescriptor
            {
                Key = "Xml",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (XmlMessageSerializer).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor Json = new RunDescriptor
            {
                Key = "Json",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (JsonMessageSerializer).AssemblyQualifiedName
                            }
                        }
            };
    }
}