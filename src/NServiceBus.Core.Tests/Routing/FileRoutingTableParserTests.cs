namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Xml.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class FileRoutingTableParserTests
    {
        [Test]
        public void It_can_parse_valid_file()
        {
            const string xml = @"
<endpoints>
    <endpoint name=""A"">
        <instance discriminator=""D1"" prop1=""V1"" prop2=""V2""/>
        <instance prop3=""V3"" prop4=""V4""/>
    </endpoint>
    <endpoint name=""B"">
        <instance discriminator=""D2"" prop5=""V5"" prop6=""V6""/>
    </endpoint>
</endpoints>
";
            var doc = XDocument.Parse(xml);
            var result = new FileRoutingTableParser().Parse(doc);
            
            CollectionAssert.AreEqual(new[]
            {
                new EndpointInstance("A", "D1").SetProperty("prop1", "V1").SetProperty("prop2","V2"), 
                new EndpointInstance("A").SetProperty("prop3", "V3").SetProperty("prop4", "V4"),
                new EndpointInstance("B", "D2").SetProperty("prop5", "V5").SetProperty("prop6", "V6"),  
            }, result);
        }

        [Test]
        public void It_requires_endpoint_name()
        {
            const string xml = @"
<endpoints>
    <endpoint someUnusedAttribute=""A""/>
</endpoints>
";
            var doc = XDocument.Parse(xml);
            try
            {
                new FileRoutingTableParser().Parse(doc);
                Assert.Fail("Expected error.");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }
    }
}