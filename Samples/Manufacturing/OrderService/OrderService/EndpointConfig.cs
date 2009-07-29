using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Host;
using NServiceBus.Sagas.Impl;

namespace OrderService
{
    public class EndpointConfig : IConfigureThisEndpoint,
                                    As.aPublisher,
                                    As.aSagaHost,
                                    ISpecify.ToUseNHibernateSubscriptionStorage,
                                    ISpecify.ToUseXmlSerialization,
                                    ISpecify.MessageHandlerOrdering
    {
        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>
              .Then<SagaMessageHandler>());
        }
    }
}