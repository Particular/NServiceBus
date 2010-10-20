using System;
using NServiceBus;

namespace CashierContracts
{
    [Serializable]
    public class NewOrderMessage : IMessage
    {
        public String CustomerName { get; private set; }
        public String Drink { get; private set; }
        public DrinkSize DrinkSize { get; private set; }
        public Guid OrderId { get; private set; }
    
        public NewOrderMessage(String customerName, String drink, DrinkSize drinkSize)
        {
            CustomerName = customerName;
            Drink = drink;
            DrinkSize = drinkSize;
            OrderId = Guid.NewGuid();
        }
    }
}
