using System;

namespace Barista.ViewData
{
    public class OrderIsDoneView
    {
        public String CustomerName { get; private set; }

        public OrderIsDoneView(String customerName)
        {
            CustomerName = customerName;
        }            
    }
}
