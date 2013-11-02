namespace NServiceBus.PowerShell
{
    using System.Collections;
    using System.Linq;
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusAuditConfig")]
    public class AddAuditConfig : AddConfigSection
    {
        const string Instructions = @"<AuditConfig 
    QueueName=""The address to which messages received will be forwarded.""
    OverrideTimeToBeReceived=""The time to be received set on forwarded messages, specified as a timespan see http://msdn.microsoft.com/en-us/library/vstudio/se73z7b9.aspx""  />";

        const string exampleAuditConfigSection = @"<section name=""AuditConfig"" type=""NServiceBus.Config.AuditConfig, NServiceBus.Core"" />";

        public override void ModifyConfig(XDocument doc)
        {
            // Add the new audit config section, if the ForwardReceivedMessagesTo attribute has not been set in the UnicastBusConfig.
            var frmAttributeEnumerator = (IEnumerable)doc.XPathEvaluate("/configuration/UnicastBusConfig/@ForwardReceivedMessagesTo");
            var isForwardReceivedMessagesAttributeDefined = frmAttributeEnumerator.Cast<XAttribute>().Any();

            // Then add the audit config
            var sectionElement =
                doc.XPathSelectElement(
                    "/configuration/configSections/section[@name='AuditConfig' and @type='NServiceBus.Config.AuditConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {
                if (isForwardReceivedMessagesAttributeDefined)
                    doc.XPathSelectElement("/configuration/configSections").Add(new XComment(exampleAuditConfigSection));
                else
                    doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                    new XAttribute("name",
                        "AuditConfig"),
                    new XAttribute("type",
                        "NServiceBus.Config.AuditConfig, NServiceBus.Core")));
            }

            var forwardingElement = doc.XPathSelectElement("/configuration/AuditConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XComment(Instructions),
                    isForwardReceivedMessagesAttributeDefined ? (object) new XComment(@"Since we detected that you already have forwarding setup we haven't enabled the audit feature.
Please remove the ForwardReceivedMessagesTo attribute from the UnicastBusConfig and uncomment the AuditConfig section. 
<AuditConfig QueueName=""audit"" />") : new XElement("AuditConfig", new XAttribute("QueueName", "audit")));
            }

        }
    }
}
