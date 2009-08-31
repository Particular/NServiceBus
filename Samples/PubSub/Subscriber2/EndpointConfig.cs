using NServiceBus.Host;
using NServiceBus.ObjectBuilder.Unity;

namespace Subscriber2
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer,
        ISpecify.ToUse.XmlSerialization, 
        ISpecify.ToUse.ContainerType<UnityObjectBuilder>, //just to show we can mix and match containers
        IDontWant.ToSubscribeAutomatically,
        ISpecify.ToRun<Subscriber2Endpoint>
    {
    }
}
