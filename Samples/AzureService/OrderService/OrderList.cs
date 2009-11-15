using System;
using System.Collections.Generic;

using System.Linq;
using MyMessages;

namespace OrderService
{
    public class OrderList
    {
        private readonly IList<Order> orders;

        public OrderList()
        {
                //add some orders to simulate "existing orders"
            orders = new List<Order>
                         {
                             new Order {Id = Guid.NewGuid(), Quantity = 20, Status = OrderStatus.Approved},
                             new Order {Id = Guid.NewGuid(), Quantity = 300, Status = OrderStatus.Approved}
                         };
        }

        public void AddOrder(Order order)
        {
            lock(orders)
                orders.Add(order);
        }

        public Order UpdateStatus(Order order,OrderStatus newStatus)
        {
            lock (orders)
            {
                foreach (var orderToUpdate in orders)
                {
                    if(orderToUpdate.Id == order.Id)
                    {
                        orderToUpdate.Status = newStatus;
                        return orderToUpdate;
                    }
                        
                }
                throw new Exception("Order not found");
            }
                
        }

        

        public IEnumerable<Order> GetOrdersToApprove()
        {
            lock(orders)
            {
                return new List<Order>(orders.Where(x => x.Status == OrderStatus.AwaitingApproval));
            }
        }

        public IEnumerable<Order> GetCurrentOrders()
        {
            lock (orders)
                return new List<Order>(orders);
        }
    }
}