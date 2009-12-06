using System;
using CashierContracts;

namespace Cashier.ViewData
{
    public class ReceivedFullPaymentView
    {
        public String CustomerName { get; private set; }
        public String Drink { get; private set; }
        public String DrinkSize { get; private set; }

        public ReceivedFullPaymentView(String customerName, String drink, DrinkSize drinkSize)
        {
            CustomerName = customerName;
            Drink = drink;
            DrinkSize = drinkSize.ToString();
        }
    }
}
