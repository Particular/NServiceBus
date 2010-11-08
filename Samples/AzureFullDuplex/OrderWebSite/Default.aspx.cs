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
            lock (WebRole.Orders)
                OrderList.DataSource = new List<Order>(WebRole.Orders);

            OrderList.DataBind();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            WebRole.Bus
                .Send(new SubmitOrderRequest
                {
                    Id = Guid.NewGuid(),
                    Quantity = Convert.ToInt32(txtQuatity.Text)
                });
        }

        public void Handle(SubmitOrderResponse message)
        {
            lock (WebRole.Orders)
                WebRole.Orders.Add(message.Order);
        }
    }
}
