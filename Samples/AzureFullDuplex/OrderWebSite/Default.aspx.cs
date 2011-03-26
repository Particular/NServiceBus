using System;
using System.Collections.Generic;
using System.Threading;
using MyMessages;
using NServiceBus;
using Order = MyMessages.Order;

namespace OrderWebSite
{
    public partial class _Default : System.Web.UI.Page, IHandleMessages<SubmitOrderResponse>
    {
        protected void Page_PreRender(object sender, EventArgs e)
        {
            lock (Global.Orders)
                OrderList.DataSource = new List<Order>(Global.Orders);

            OrderList.DataBind();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            Global.Bus
                .Send(new SubmitOrderRequest
                {
                    Id = Guid.NewGuid(),
                    Quantity = Convert.ToInt32(txtQuatity.Text)
                });
        }

        public void Handle(SubmitOrderResponse message)
        {
            lock (Global.Orders)
                Global.Orders.Add(message.Order);
        }
    }
}
