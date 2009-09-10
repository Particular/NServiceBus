using NServiceBus.Host;
using NServiceBus.ObjectBuilder.Unity;

namespace Subscriber2
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer,
        ISpecify.ToUse.XmlSerialization, 
        //ISpecify.ToUse.ContainerType<UnityObjectBuilder>, //there are issues with this container - do not use
        IDontWant.ToSubscribeAutomatically
    {
    }
}
