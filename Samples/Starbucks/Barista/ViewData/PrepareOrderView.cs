using System;
using CashierContracts;

namespace Barista.ViewData
{
    public class PrepareOrderView
    {
        public String CustomerName { get; private set; }
        public String Drink { get; private set; }
        public String DrinkSize { get; private set; }

        public PrepareOrderView(String customerName, String drink, DrinkSize size)
        {
            CustomerName = customerName;
            Drink = drink;
            DrinkSize = size.ToString();
        }
    }
}
