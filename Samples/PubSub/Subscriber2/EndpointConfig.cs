using NServiceBus.Host;
using NServiceBus.ObjectBuilder.Unity;

namespace Subscriber2
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer,
        ISpecify.ToUseXmlSerialization, 
        ISpecify.ContainerTypeToUse<UnityObjectBuilder>, //just to show we can mix and match containers
        IDontWantToSubscribeAutomatically,
        ISpecify.ToRun<Subscriber2Endpoint>
    {
    }
}
