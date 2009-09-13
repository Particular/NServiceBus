using System.Configuration;
using NServiceBus;
using NServiceBus.Host;

namespace Timeout.MessageHandlers
{
    public class Endpoint : IConfigureThisEndpoint, AsA_Server, ISpecifyProfile<TimeoutProfile> {}

    public class TimeoutProfile : Lite {}

    public class NsbConfig : IConfigureTheBusForProfile<TimeoutProfile>
    {
        void IConfigureTheBus.Configure(IConfigureThisEndpoint specifier)
        {
            var configure = NServiceBus.Configure.With().SpringBuilder();

            string nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            string serialization = ConfigurationManager.AppSettings["Serialization"];

            switch (serialization)
            {
                case "xml":
                    configure.XmlSerializer(nameSpace);
                    break;
                case "binary":
                    configure.BinarySerializer();
                    break;
                default:
                    throw new ConfigurationErrorsException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
            }
        }
    }
}
