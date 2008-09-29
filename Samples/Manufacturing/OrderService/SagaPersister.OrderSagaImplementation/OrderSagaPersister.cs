using System;
using System.Collections.Generic;
using NServiceBus.Saga;
using OrderService.MessageHandlers;
using OrderService;

namespace SagaPersister.OrderSagaImplementation
{
    public class OrderSagaPersister : ISagaPersister, IQuerySagasByPartnerIdAndPurchaseOrderNumber
    {
        public void Save(ISagaEntity saga)
        {
            storage[saga.Id] = saga;
        }

        public void Update(ISagaEntity saga)
        {
            storage[saga.Id] = saga;
        }

        public ISagaEntity Get(Guid sagaId)
        {
            ISagaEntity result;
            storage.TryGetValue(sagaId, out result);

            return result;
        }

        public void Complete(ISagaEntity saga)
        {
            storage.Remove(saga.Id);
        }

        public void Dispose()
        {
        }

        public ISagaEntity Query(Guid partnerId, string purchaseOrderNumber)
        {
            foreach(ISagaEntity saga in storage.Values)
            {
                OrderSaga os = saga as OrderSaga;
                if (os == null)
                    continue;

                if (os.PartnerId == partnerId && os.PurchaseOrderNumber == purchaseOrderNumber)
                    return os;
            }

            return null;
        }

        private static readonly Dictionary<Guid, ISagaEntity> storage = new Dictionary<Guid, ISagaEntity>();
    }
}
