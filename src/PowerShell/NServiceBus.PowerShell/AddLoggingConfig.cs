namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusLoggingConfig")]
    public class AddLoggingConfig : AddConfigSection
    {
        const string Instructions = @"<Logging
    Threshold=""The desired log level."" />";

        public override void ModifyConfig(XDocument doc)
        {
            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='Logging' and @type='NServiceBus.Config.Logging, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                    new XAttribute("name", "Logging"),
                    new XAttribute("type", "NServiceBus.Config.Logging, NServiceBus.Core")));
            }

            var forwardingElement = doc.XPathSelectElement("/configuration/Logging");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XComment(Instructions),
                                               new XElement("Logging",
                                                            new XAttribute("Threshold", "INFO")));
            }
        }
    }
}