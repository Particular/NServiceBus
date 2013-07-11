using System;
using NServiceBus;

namespace CustomerContracts
{
    public class OrderReadyMessage : IEvent
    {
        public String CustomerName { get; set; }
        public String Drink { get; set; }

        public OrderReadyMessage(String customerName, String drink)
        {
            CustomerName = customerName;
            Drink = drink;
        }
    }
}
