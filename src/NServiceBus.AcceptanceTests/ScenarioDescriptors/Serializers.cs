namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;

    public static class Serializers
    {

        public static readonly RunDescriptor Xml = new RunDescriptor
            {
                Key = "Xml",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Serializer", typeof (XmlSerializer).AssemblyQualifiedName
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
                                "Serializer", typeof (JsonSerializer).AssemblyQualifiedName
                            }
                        }
            };
    }
}