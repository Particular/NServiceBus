using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class EndpointConfig:IConfigureThisEndpoint,
                                As.aServer,
                                ISpecify.ToUse.XmlSerialization,
                                ISpecify.XmlSerializationNamespace,
                                IWantCustomInitialization
    {
        public string Namespace
        {
            get { return "http://www.UdiDahan.com"; }
        }

        public void Init(Configure configure)
        {
            configure.RijndaelEncryptionService();
        }
    }
}