using System;
using System.Windows.Forms;
using Cashier.ViewData;

namespace Cashier
{
    public interface IStarbucksCashierView
    {
        void CustomerRefusesToPay(CustomerRefusesToPayView view);
        void NewOrder(NewOrderView view);
        void ReceivedFullPayment(ReceivedFullPaymentView view);
        void Start();
    }

    public partial class StarbucksCashier : Form, IStarbucksCashierView
    {
        public StarbucksCashier()
        {
            InitializeComponent();
        }

        public void CustomerRefusesToPay(CustomerRefusesToPayView view)
        {
            var logItem = String.Format("Customer {0} refuses to pay €{1} for its order ({2} - {3}).",
                                        view.CustomerName, view.Amount, view.Drink, view.DrinkSize);

            Invoke(new Action<String>(Log), logItem);
        }

        public void NewOrder(NewOrderView view)
        {
            var logItem = String.Format("Customer {0} ordered {1} - {2}.",
                                        view.CustomerName, view.Drink, view.DrinkSize);
                                        
            Invoke(new Action<String>(Log), logItem);
        }

        public void ReceivedFullPayment(ReceivedFullPaymentView view)
        {
            var logItem = String.Format("Customer {0} paid for its order ({1} - {2}).",
                                        view.CustomerName, view.Drink, view.DrinkSize);
            
            Invoke(new Action<String>(Log), logItem);
        }

        public void Start()
        {
            Application.Run(this);
        }
                
        private void Log(String logItem)
        {
            OrdersListBox.Items.Add(logItem);
        }
    }
}
