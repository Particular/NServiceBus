using System.ServiceModel;
using System.ServiceModel.Channels;
using NServiceBus.Host;

namespace Server
{
    internal class EndpointConfig : IConfigureThisEndpoint, 
                                    ISpecifyDefaultWcfBinding,
                                    AsA_Publisher
    {
        public Binding SpecifyBinding()
        {
            return new BasicHttpBinding();
        }
    }
}