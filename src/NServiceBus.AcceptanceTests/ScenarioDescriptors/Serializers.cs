namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using AcceptanceTesting.Support;

    public static class Serializers
    {
        public static RunDescriptor Xml
        {
            get
            {
                var xml = new RunDescriptor("Xml");
                xml.Settings.Set("Serializer", typeof(XmlSerializer));
                return xml;
            }
        }

        public static RunDescriptor Json
        {
            get
            {
                var json = new RunDescriptor("Json");
                json.Settings.Set("Serializer", typeof(JsonSerializer));
                return json;
            }
        }
    }
}