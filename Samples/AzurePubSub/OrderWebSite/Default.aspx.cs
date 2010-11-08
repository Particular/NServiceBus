using System;
using System.Collections.Generic;
using MyMessages;

namespace OrderWebSite
{
    public partial class _Default : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            lock (WebRole.Orders)
                OrderList.DataSource = new List<Order>(WebRole.Orders);


            OrderList.DataBind();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            var order = new Order
                            {
                                Id = Guid.NewGuid(),
                                Quantity = Convert.ToInt32(txtQuatity.Text),
                                Status = OrderStatus.Pending

                            };

            WebRole.Bus.Send(new OrderMessage
                                {
                                    Id = order.Id, 
                                    Quantity = order.Quantity
                                });

            lock (WebRole.Orders)
                WebRole.Orders.Add(order);

            Refresh();
        }
    }
}
