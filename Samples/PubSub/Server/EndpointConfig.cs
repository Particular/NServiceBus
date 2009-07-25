using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Host;
using NServiceBus.Sagas.Impl;

namespace Server
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aServer, As.aPublisher,
        ISpecify.ToUseXmlSerialization,
        ISpecify.ToRun<ServerEndpoint>,
        ISpecify.MessageHandlerOrdering
    {
        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>());
        }
    }
}
