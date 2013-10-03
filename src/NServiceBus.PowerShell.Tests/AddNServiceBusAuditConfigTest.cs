namespace NServiceBus.PowerShell.Tests
{
    using System.Xml.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class AddNServiceBusAuditConfigTest
    {
        [Test]
        public void When_ForwardReceivedMessagesTo_Attribute_Is_Not_Defined_AuditSection_Should_Be_Added()
        {
            const string appConfigString = @"<configuration>
  <configSections>
    <section name=""UnicastBusConfig"" type=""NServiceBus.Config.UnicastBusConfig, NServiceBus.Core"" />
  </configSections>
  <UnicastBusConfig>
    <MessageEndpointMappings />
  </UnicastBusConfig>
</configuration>";

            const string expectedConfigurationChange = @"<configuration>
  <configSections>
    <section name=""UnicastBusConfig"" type=""NServiceBus.Config.UnicastBusConfig, NServiceBus.Core"" />
    <section name=""AuditConfig"" type=""NServiceBus.Config.AuditConfig, NServiceBus.Core"" />
  </configSections>
  <UnicastBusConfig>
    <MessageEndpointMappings />
  </UnicastBusConfig>
  <!--<AuditConfig 
    QueueName=""The address to which messages received will be forwarded.""
    OverrideTimeToBeReceived=""The time to be received set on forwarded messages, specified as a timespan see http://msdn.microsoft.com/en-us/library/vstudio/se73z7b9.aspx""  />-->
  <AuditConfig QueueName=""audit"" />
</configuration>";

            var xDocument = XDocument.Parse(appConfigString);
            var addAuditConfig = new AddAuditConfig();
            addAuditConfig.ModifyConfig(xDocument);
            var generatedXml = xDocument.ToString();

            Assert.IsNotNull(xDocument);
            Assert.AreEqual(expectedConfigurationChange, generatedXml, string.Format("Generated Xml: {0}", generatedXml));
        }

        [Test]
        public void When_ForwardReceivedMessagesTo_Attribute_Is_Defined_AuditSection_Should_Not_Be_Added()
        {
            const string appConfigString = @"<configuration>
  <configSections>
    <section name=""UnicastBusConfig"" type=""NServiceBus.Config.UnicastBusConfig, NServiceBus.Core"" />
  </configSections>
  <UnicastBusConfig ForwardReceivedMessagesTo=""audit"">
    <MessageEndpointMappings />
  </UnicastBusConfig>
</configuration>";

            var xDocument = XDocument.Parse(appConfigString);
            var addAuditConfig = new AddAuditConfig();
            addAuditConfig.ModifyConfig(xDocument);
            var generatedXml = xDocument.ToString();

            Assert.IsNotNull(xDocument);
            Assert.AreEqual(appConfigString, generatedXml, string.Format("Generated Xml: {0}", generatedXml));
        }
    }
}
