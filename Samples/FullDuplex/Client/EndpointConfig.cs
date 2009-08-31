using NServiceBus;
using NServiceBus.Host;

namespace Client
{
    public class EndpointConfig:IConfigureThisEndpoint,
                                As.aClient,
                                ISpecify.ToUse.XmlSerialization,
                                ISpecify.XmlSerializationNamespace,
                                IDontWant.Sagas,
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