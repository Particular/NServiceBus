using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class EndpointConfig:IConfigureThisEndpoint,
                                As.aServer,
                                ISpecify.ToUseXmlSerialization,
                                ISpecify.XmlSerializationNamespace,
                                IWantCustomInitialization,
                                IDontWant.Sagas
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