using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Host;
using NServiceBus.Sagas.Impl;

namespace Server
{
    class EndpointConfig : IConfigureThisEndpoint, 
        As.aPublisher,
        ISpecify.ToUse.XmlSerialization,
        ISpecify.ToRun<ServerEndpoint>,
        ISpecify.MessageHandlerOrdering,
        ISpecify.ToUse.SubscriptionAuthorizer<SubscriptionAuthorizer>
    {
        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>.Then<SagaMessageHandler>());
        }
    }
}
