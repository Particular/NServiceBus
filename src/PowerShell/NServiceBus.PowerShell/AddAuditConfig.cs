namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusAuditConfig")]
    public class AddAuditConfig : AddConfigSection
    {
        const string Instructions = @"<AuditConfig 
    QueueName=""The address to which messages received will be forwarded.""
    OverrideTimeToBeRecieved=""The time to be received set on forwarded messages, specified as a timespan see http://msdn.microsoft.com/en-us/library/vstudio/se73z7b9.aspx""  />";

        public override void ModifyConfig(XDocument doc)
        {
            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='AuditConfig' and @type='NServiceBus.Config.AuditConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                                                                                         new XAttribute("name",
                                                                                                        "AuditConfig"),
                                                                                         new XAttribute("type",
                                                                                                        "NServiceBus.Config.AuditConfig, NServiceBus.Core")));

            }

            var forwardingElement = doc.XPathSelectElement("/configuration/AuditConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(
                                                new XComment(Instructions),
                                                new XElement("AuditConfig",
                                                new XAttribute("QueueName", "audit")
                                                ));
            }
        }
    }
}