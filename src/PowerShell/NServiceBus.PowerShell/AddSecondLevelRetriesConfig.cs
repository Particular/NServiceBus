namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusSecondLevelRetriesConfig")]
    public class AddSecondLevelRetriesConfig : AddConfigSection
    {
        const string Instructions = @"<SecondLevelRetriesConfig
    Enabled=""Set to false to disable second-level retries.""
    NumberOfRetries=""The number of SLR rounds to attempt after the initial set of first-level retries.""
    TimeIncrease=""The amount of additional time to wait before every successive SLR round."" />";

        public override void ModifyConfig(XDocument doc)
        {
            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='SecondLevelRetriesConfig' and @type='NServiceBus.Config.SecondLevelRetriesConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                    new XAttribute("name", "SecondLevelRetriesConfig"),
                    new XAttribute("type", "NServiceBus.Config.SecondLevelRetriesConfig, NServiceBus.Core")));
            }

            var forwardingElement = doc.XPathSelectElement("/configuration/SecondLevelRetriesConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XComment(Instructions),
                                               new XElement("SecondLevelRetriesConfig",
                                                            new XAttribute("Enabled", "true"),
                                                            new XAttribute("NumberOfRetries", "3"),
                                                            new XAttribute("TimeIncrease", "00:00:10")));
            }
        }
    }
}