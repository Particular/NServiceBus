namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Routing;

    class FileRoutingTableParser
    {
        public FileRoutingTableParser()
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream("NServiceBus.Routing.FileBasedDynamicRouting.endpoints.xsd"))
            using (var xmlReader = XmlReader.Create(stream))
            {
                schema = new XmlSchemaSet();
                schema.Add("", xmlReader);
            }
        }

        public IEnumerable<EndpointInstance> Parse(XDocument document)
        {
            document.Validate(schema, null, true);

            var root = document.Root;
            var endpointElements = root.Descendants("endpoint");

            var instances = new List<EndpointInstance>();

            foreach (var e in endpointElements)
            {
                var endpointName = e.Attribute("name").Value;

                foreach (var i in e.Descendants("instance"))
                {
                    var instanceAddress = endpointName;

                    var discriminatorAttribute = i.Attribute("discriminator");
                    var discriminator = discriminatorAttribute?.Value;
                    if (!string.IsNullOrWhiteSpace(discriminator))
                    {
                        instanceAddress += $"-{discriminator}";
                    }


                    var machineAttribute = i.Attribute("machine");
                    var machine = machineAttribute?.Value;
                    if (!string.IsNullOrWhiteSpace(machine))
                    {
                        instanceAddress += $"@{machine}";
                    }

                    instances.Add(new EndpointInstance(endpointName, instanceAddress));
                }
            }

            return instances;
        }

        XmlSchemaSet schema;
    }
}