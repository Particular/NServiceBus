using NServiceBus.Host;
using NServiceBus.ObjectBuilder.Unity;

namespace Subscriber2
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer,
        ISpecify.ToUse.XmlSerialization, 
        ISpecify.ToUse.ContainerType<UnityObjectBuilder>, //just to show we can mix & match containers
        IDontWant.ToSubscribeAutomatically
    {
    }
}
