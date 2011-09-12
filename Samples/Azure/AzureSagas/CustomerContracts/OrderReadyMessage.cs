using System;
using NServiceBus;

namespace CustomerContracts
{
 public class OrderReadyMessage : IMessage
    {
        public String CustomerName { get; set; }
        public String Drink { get; set; }

        public OrderReadyMessage()
        {
        }

     public OrderReadyMessage(String customerName, String drink)
        {
            CustomerName = customerName;
            Drink = drink;
        }
    }
}
