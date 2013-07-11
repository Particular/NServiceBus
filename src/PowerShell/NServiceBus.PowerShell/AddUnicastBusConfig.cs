namespace NServiceBus.PowerShell
{
    using System.Management.Automation;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusUnicastBusConfig")]
    public class AddUnicastBusConfig : AddConfigSection
    {
        const string Instructions = @"<UnicastBusConfig 
    ForwardReceivedMessagesTo=""The address to which messages received will be forwarded.""
    DistributorControlAddress=""The address for sending control messages to the distributor.""
    DistributorDataAddress=""The distributor's data address, used as the return address of messages sent by this endpoint.""
    TimeToBeReceivedOnForwardedMessages=""The time to be received set on forwarded messages, specified as a timespan see http://msdn.microsoft.com/en-us/library/vstudio/se73z7b9.aspx""
    TimeoutManagerAddress=""The address that the timeout manager will use to send and receive messages."" >
    <MessageEndpointMappings>
      To register all message types defined in an assembly:
      <add Assembly=""assembly"" Endpoint=""queue@machinename"" />
      
      To register all message types defined in an assembly with a specific namespace (it does not include sub namespaces):
      <add Assembly=""assembly"" Namespace=""namespace"" Endpoint=""queue@machinename"" />
      
      To register a specific type in an assembly:
      <add Assembly=""assembly"" Type=""type fullname (http://msdn.microsoft.com/en-us/library/system.type.fullname.aspx)"" Endpoint=""queue@machinename"" />
    </MessageEndpointMappings>
  </UnicastBusConfig>";

        public override void ModifyConfig(XDocument doc)
        {
            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='UnicastBusConfig' and @type='NServiceBus.Config.UnicastBusConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                                                                                         new XAttribute("name",
                                                                                                        "UnicastBusConfig"),
                                                                                         new XAttribute("type",
                                                                                                        "NServiceBus.Config.UnicastBusConfig, NServiceBus.Core")));

            }

            var forwardingElement = doc.XPathSelectElement("/configuration/UnicastBusConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(
                                                new XComment(Instructions), 
                                                new XElement("UnicastBusConfig",
                                                new XAttribute("ForwardReceivedMessagesTo", "audit"),
                                                new XElement("MessageEndpointMappings")));
            }
        }
    }
}