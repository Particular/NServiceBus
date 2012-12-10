using CashierContracts;

namespace Barista.ViewData
{
    public class OrderIsTrashedView
    {
        private readonly string drink;
        private readonly string customerName;
        private readonly DrinkSize size;

        public OrderIsTrashedView(string drink, string customerName, DrinkSize size)
        {
            this.drink = drink;
            this.customerName = customerName;
            this.size = size;
        }

        public DrinkSize Size
        {
            get { return size; }
        }

        public string CustomerName
        {
            get { return customerName; }
        }

        public string Drink
        {
            get { return drink; }
        }
    }
}