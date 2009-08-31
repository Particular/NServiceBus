using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Host;
using NServiceBus.Sagas.Impl;

namespace OrderService
{
    public class EndpointConfig : IConfigureThisEndpoint,
                                    As.aPublisher,
                                    ISpecify.ToUse.XmlSerialization,
                                    ISpecify.MessageHandlerOrdering
    {
        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>
              .Then<SagaMessageHandler>());
        }
    }
}