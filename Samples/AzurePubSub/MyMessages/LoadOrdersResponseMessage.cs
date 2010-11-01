using System.Collections.Generic;
using NServiceBus;

namespace MyMessages
{
    public class LoadOrdersResponseMessage : IMessage
    {
        private List<Order> orders;

        public List<Order> Orders
        {
            get
            {
                if(orders == null)
                    orders = new List<Order>();

                return orders;
            }
            set
            {
                orders = value;
            }
        }
    }
}