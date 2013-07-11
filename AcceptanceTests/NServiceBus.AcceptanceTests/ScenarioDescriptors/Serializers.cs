namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;
    using NServiceBus.Serializers.Binary;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Serializers.XML;

    public static class Serializers
    {
        public static readonly RunDescriptor Binary = new RunDescriptor
            {
                Key = "Binary",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (BinaryMessageSerializer).AssemblyQualifiedName
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