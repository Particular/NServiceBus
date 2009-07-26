using System;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Host.Tests
{
    public class ServerEndpointConfig : IConfigureThisEndpoint,
                                        As.aServer ,
                                        As.aPublisher  
    {
    }

    public class ServerEndpointConfigWithCustomSagaPersister : IConfigureThisEndpoint,
                                    As.aServer,
                                    ISpecify.MyOwnSagaPersistence
    {
        public void Init(Configure configure)
        {
            configure.Configurer.ConfigureComponent<FakePersister>(ComponentCallModelEnum.Singleton);
        }
    }

    public class FakePersister
    {
    }
}