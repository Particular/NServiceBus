namespace NServiceBus.PowerShell.Tests
{
    using System.Xml.Linq;
    using System.Xml.XPath;
    using NUnit.Framework;

    [TestFixture]
    public class AddNServiceBusAuditConfigTest
    {
        [Test]
        public void AreNecessaryAttributesForAuditSectionAdded()
        {
            const string appConfigString = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
                                    <configuration>
                                        <configSections></configSections>
                                    </configuration>";

            var xdoc = XDocument.Parse(appConfigString);
            var addAuditConfig = new AddAuditConfig();
            addAuditConfig.ModifyConfig(xdoc);

            // Does the xmlDocument contain the new config section & attributes?
            var sectionElement = xdoc.XPathSelectElement("/configuration/configSections/section[@name='AuditConfig' and @type='NServiceBus.Config.AuditConfig, NServiceBus.Core']");
            Assert.IsNotNull(sectionElement);

            var configElement = xdoc.XPathSelectElement("/configuration/AuditConfig").Attribute("QueueName");
            Assert.IsNotNull(configElement);
            Assert.AreEqual(configElement.Value, "audit");

        }

    }
}
