using NServiceBus;
using NServiceBus.Host;

namespace Client
{
    public class EndpointConfig:IConfigureThisEndpoint,
                                AsA_Client,
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