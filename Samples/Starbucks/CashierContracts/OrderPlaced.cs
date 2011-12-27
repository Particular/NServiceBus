using System;
using NServiceBus;

namespace CashierContracts
{
    [Serializable]
    public class OrderPlaced : IMessage
    {
        public String CustomerName { get; private set; }
        public String Drink { get; private set; }
        public DrinkSize DrinkSize { get; private set; }
        public Guid OrderId { get; private set; }

        public OrderPlaced(String customerName, String drink, DrinkSize size, Guid orderId)
        {
            CustomerName = customerName;
            Drink = drink;
            DrinkSize = size;
            OrderId = orderId;
        }
    }
}
