using System;
using CashierContracts;

namespace Barista.ViewData
{
    public class DeliverOrderView
    {
        public String Drink { get; private set; }
        public String DrinkSize { get; private set; }

        public DeliverOrderView(String drink, DrinkSize size)
        {
            Drink = drink;
            DrinkSize = size.ToString();
        }
    }
}
