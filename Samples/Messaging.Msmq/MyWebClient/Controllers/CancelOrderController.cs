namespace MyWebClient.Controllers
{
    using System.Net;
    using System.Web.Mvc;
    using MyMessages.Commands;
    using NServiceBus;

    public class CancelOrderController : AsyncController
    {
        public IBus Bus { get; set; }

        [AsyncTimeout(30000)]
        public void IndexAsync(int orderNumber)
        {
            
            var command = new CancelOrder
                {
                    OrderNumber = orderNumber
                };

            Bus.Send(command).Register<OrderStatus>(status =>
                {
                    AsyncManager.Parameters["status"] = status;
                    AsyncManager.Parameters["orderNumber"] = command.OrderNumber;
                }, this);
        }

        public ActionResult IndexCompleted(OrderStatus status, int orderNumber)
        {
            if (status == OrderStatus.Failed)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }

            return new JsonResult
                {
                    Data = new
                        {
                            OrderNumber = orderNumber,
                        }
                };
        }
    }
}