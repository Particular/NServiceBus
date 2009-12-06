using System;
using CashierContracts;
using NServiceBus.Saga;

namespace Cashier
{
    public class CashierSagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual String Originator { get; set; }
        public virtual String OriginalMessageId { get; set; }

        public virtual Decimal Amount { get; set; }
        public virtual String CustomerName { get; set; }
        public virtual String Drink { get; set; }
        public virtual DrinkSize DrinkSize { get; set; }
        public virtual Guid OrderId { get; set; }
    }
}
