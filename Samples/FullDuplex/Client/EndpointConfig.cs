using NServiceBus;
using NServiceBus.Host;

namespace Client
{
    public class EndpointConfig:IConfigureThisEndpoint,
                                As.aClient,
                                ISpecify.ToUseXmlSerialization,
                                ISpecify.XmlSerializationNamespace,
                                IWantCustomInitialization,
                                ISpecify.ToRun<ClientEndpoint>
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