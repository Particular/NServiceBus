using System;
using System.Windows.Forms;
using Barista.ViewData;

namespace Barista
{
    public interface IStarbucksBaristaView
    {
        void DeliverOrder(DeliverOrderView view);
        void OrderIsDone(OrderIsDoneView view);
        void PrepareOrder(PrepareOrderView view);
        void Start();
    }

    public partial class StarbucksBarista : Form, IStarbucksBaristaView
    {
        public StarbucksBarista()
        {
            InitializeComponent();
        }

        public void DeliverOrder(DeliverOrderView view)
        {
            var logItem = String.Format("Delivering {0} - {1}.", view.Drink, view.DrinkSize);
            Invoke(new Action<String>(Log), logItem);    
        }

        public void OrderIsDone(OrderIsDoneView view)
        {
            var logItem = String.Format("Done preparing order for customer {0}.", view.CustomerName);
            Invoke(new Action<String>(Log), logItem);
        }

        public void PrepareOrder(PrepareOrderView view)
        {
            var logItem = String.Format("Preparing {0} - {1} for customer {2}.", 
                                        view.Drink, view.DrinkSize, view.CustomerName);
            
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
