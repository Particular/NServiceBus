namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusMasterNodeConfig")]
    public class AddMasterNodeConfig : AddConfigSection
    {
        const string Instructions = @"<MasterNodeConfig
    Node=""The server name where the Master Node is located."" />";

        public override void ModifyConfig(XDocument doc)
        {
            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='MasterNodeConfig' and @type='NServiceBus.Config.MasterNodeConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                    new XAttribute("name", "MasterNodeConfig"),
                    new XAttribute("type", "NServiceBus.Config.MasterNodeConfig, NServiceBus.Core")));
            }

            var forwardingElement = doc.XPathSelectElement("/configuration/MasterNodeConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XComment(Instructions),
                                               new XElement("MasterNodeConfig",
                                                            new XAttribute("Node", "SERVER_NAME_HERE")));
            }
        }
    }
}