using System;
using CashierContracts;
using NServiceBus.Saga;

namespace Barista
{
    public class BaristaSagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual String Originator { get; set; }
        public virtual String OriginalMessageId { get; set; }

        public virtual String CustomerName { get; set; }
        public virtual String Drink { get; set; }
        public virtual Guid OrderId { get; set; }
        public virtual DrinkSize Size { get; set; }
        public virtual Boolean OrderIsReady { get; set; }
        public virtual Boolean OrderIsPaid { get; set; }
    }
}
