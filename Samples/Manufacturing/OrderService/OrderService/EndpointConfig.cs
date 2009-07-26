using System;
using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Host;
using NServiceBus.Saga;
using NServiceBus.Sagas.Impl;
using Configure=NServiceBus.Configure;

namespace OrderService
{
    public class EndpointConfig : IConfigureThisEndpoint,
                                    As.aServer,
                                    ISpecify.MyOwnSagaPersistence,
                                    ISpecify.ToUseXmlSerialization,
        ISpecify.MessageHandlerOrdering
    {
        public void Init(Configure configure)
        {
            configure.NHibernateSagaPersister();
            configure.NHibernateSubcriptionStorage();
        }

        public void SpecifyOrder(Order order)
        {
            order.Specify(First<GridInterceptingMessageHandler>
              .Then<SagaMessageHandler>());
        }
    }
}