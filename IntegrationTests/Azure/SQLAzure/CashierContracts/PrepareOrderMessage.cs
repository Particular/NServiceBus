using System;
using NServiceBus;

namespace CashierContracts
{
    public class PrepareOrderMessage : ICommand
    {
        public String CustomerName { get; set; }
        public String Drink { get; set; }
        public DrinkSize DrinkSize { get; set; }
        public Guid OrderId { get; set; }

        public PrepareOrderMessage(String customerName, String drink, DrinkSize size, Guid orderId)
        {
            CustomerName = customerName;
            Drink = drink;
            DrinkSize = size;
            OrderId = orderId;
        }
    }
}
