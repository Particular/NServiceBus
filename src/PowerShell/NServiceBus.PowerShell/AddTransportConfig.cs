namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusTransportConfig")]
    public class AddTransportConfig : AddConfigSection
    {
        const string Instructions = @"TransportConfig Settings:
      MaxRetries controls the total number of first-level tries each message is allowed.
      MaximumConcurrencyLevel controls how many threads will process messages simultaneously.
      MaximumMessageThroughputPerSecond puts a limit on how quickly messages can be processed between
          all threads. Use MaximumMessageThroughputPerSecond=""0"" to have no throughput limit.";

        public override void ModifyConfig(XDocument doc)
        {
            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='TransportConfig' and @type='NServiceBus.Config.TransportConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                    new XAttribute("name", "TransportConfig"),
                    new XAttribute("type", "NServiceBus.Config.TransportConfig, NServiceBus.Core")));
            }

            var forwardingElement = doc.XPathSelectElement("/configuration/TransportConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XComment(Instructions));
                doc.Root.LastNode.AddAfterSelf(new XElement("TransportConfig",
                        new XAttribute("MaxRetries", "5"),
                        new XAttribute("MaximumConcurrencyLevel", "1"),
                        new XAttribute("MaximumMessageThroughputPerSecond", "0")));
            }
        }
    }
}