namespace NServiceBus.Core.Tests.Routing
{
    using System.Xml.Linq;
    using System.Xml.Schema;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class FileRoutingTableParserTests
    {
        //TODO: add test for machine name

        [Test]
        public void It_can_parse_valid_file()
        {
            const string xml = @"
<endpoints>
    <endpoint name=""A"">
        <instance discriminator=""D1""/>
        <instance />
    </endpoint>
    <endpoint name=""B"">
        <instance discriminator=""D2""/>
    </endpoint>
</endpoints>
";
            var doc = XDocument.Parse(xml);
            var result = new FileRoutingTableParser().Parse(doc);

            CollectionAssert.AreEqual(new[]
            {
                new EndpointInstance("A", "A-D1"),
                new EndpointInstance("A", "A"),
                new EndpointInstance("B", "B-D2")
            }, result);
        }

        [Test]
        public void It_allows_empty_endpoints_element()
        {
            const string xml = @"
<endpoints>
</endpoints>
";
            var doc = XDocument.Parse(xml);
            var parser = new FileRoutingTableParser();

            Assert.DoesNotThrow(() => parser.Parse(doc));
        }

        [Test]
        public void It_requires_endpoint_name()
        {
            const string xml = @"
<endpoints>
    <endpoint/>
</endpoints>
";
            var doc = XDocument.Parse(xml);
            var parser = new FileRoutingTableParser();

            var exception = Assert.Throws<XmlSchemaValidationException>(() => parser.Parse(doc));
            Assert.That(exception.Message, Does.Contain("The required attribute 'name' is missing."));
        }

        [Test]
        public void It_allows_endpoint_to_not_have_an_instance()
        {
            const string xml = @"
<endpoints>
    <endpoint name=""A""/>
</endpoints>
";
            var doc = XDocument.Parse(xml);
            var parser = new FileRoutingTableParser();

            Assert.DoesNotThrow(() => parser.Parse(doc));
        }
    }
}
