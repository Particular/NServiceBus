namespace MyWebClient.Controllers
{
    using System.Net;
    using System.Threading;
    using System.Web.Mvc;
    using MyMessages.Commands;
    using NServiceBus;

    public class PlaceOrderController : AsyncController
    {
        private static int orderNumber;

        public IBus Bus { get; set; }

        [AsyncTimeout(5000)]
        public void IndexAsync(string[] videoIds)
        {
            if (videoIds == null || videoIds.Length == 0)
            {
                return;
            }

            var command = new OrderCommand
                {
                    OrderNumber = Interlocked.Increment(ref orderNumber), 
                    VideoIds = videoIds
                };

            command.SetHeader("Debug", Request.Headers["Debug"]);

            Bus.Send(command).Register<OrderStatus>(status =>
            {
                AsyncManager.Parameters["status"] = status;
                AsyncManager.Parameters["orderNumber"] = command.OrderNumber;
            }, this);
        }

        public ActionResult IndexCompleted(OrderStatus status, int orderNumber)
        {
            if (status == 0)
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
