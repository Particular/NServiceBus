using System;
using CashierContracts;

namespace Cashier.ViewData
{
    public class CustomerRefusesToPayView
    {
        public Double Amount { get; private set; }
        public String CustomerName { get; private set; }
        public String Drink { get; private set; }
        public String DrinkSize { get; private set; }

        public CustomerRefusesToPayView(String customerName, Double amount, String drink, DrinkSize drinkSize)
        {
            CustomerName = customerName;
            Amount = amount;
            Drink = drink;
            DrinkSize = drinkSize.ToString();
        }
    }
}
