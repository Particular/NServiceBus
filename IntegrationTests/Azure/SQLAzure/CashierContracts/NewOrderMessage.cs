using System;
using NServiceBus;

namespace CashierContracts
{
    public class NewOrderMessage : ICommand
    {
        public String CustomerName { get; set; }
        public String Drink { get; set; }
        public DrinkSize DrinkSize { get; set; }
        public Guid OrderId { get; set; }
    
        public NewOrderMessage(String customerName, String drink, DrinkSize drinkSize)
        {
            CustomerName = customerName;
            Drink = drink;
            DrinkSize = drinkSize;
            OrderId = Guid.NewGuid();
        }
    }
}
