namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusMessageForwardingInCaseOfFaultConfig")]
    public class AddMessageForwardingInCaseOfFaultConfig : AddConfigSection
    {
        public override void ModifyConfig(XDocument doc)
        {
            const string Instructions = @"<MessageForwardingInCaseOfFaultConfig 
    ErrorQueue=""The queue to which errors will be forwarded."" />";

            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='MessageForwardingInCaseOfFaultConfig' and @type='NServiceBus.Config.MessageForwardingInCaseOfFaultConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                                                                                         new XAttribute("name",
                                                                                                        "MessageForwardingInCaseOfFaultConfig"),
                                                                                         new XAttribute("type",
                                                                                                        "NServiceBus.Config.MessageForwardingInCaseOfFaultConfig, NServiceBus.Core")));

            }

            var forwardingElement = doc.XPathSelectElement("/configuration/MessageForwardingInCaseOfFaultConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XComment(Instructions), 
                                                new XElement("MessageForwardingInCaseOfFaultConfig",
                                                new XAttribute("ErrorQueue", "error")));
            }
        }
    }
}