using System;
using CashierContracts;

namespace Cashier.ViewData
{
    public class NewOrderView
    {
        public String CustomerName { get; private set; }
        public String Drink { get; private set; }
        public String DrinkSize { get; private set; }

        public NewOrderView(NewOrderMessage message)
        {
            CustomerName = message.CustomerName;
            Drink = message.Drink;
            DrinkSize = message.DrinkSize.ToString();
        }        
    }
}
