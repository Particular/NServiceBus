namespace MyWebClient.Controllers
{
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Web.Mvc;
    using MyMessages.Commands;
    using NServiceBus;

    public class PlaceOrderController : AsyncController
    {
        private static int orderNumber;

        public IBus Bus { get; set; }

        [AsyncTimeout(50000)]
        public void IndexAsync(string[] videoIds)
        {
            if (videoIds == null || videoIds.Length == 0)
            {
                return;
            }

            var command = new OrderCommand { OrderNumber = Interlocked.Increment(ref orderNumber) };

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
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError,
                                                "Failed to place order. This simulates a failure!");
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

    public class JsonDataContractActionResult : ActionResult
    {
        public JsonDataContractActionResult(Object data)
        {
            this.Data = data;
        }

        public Object Data { get; private set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var serializer = new DataContractJsonSerializer(this.Data.GetType());
            String output = String.Empty;
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this.Data);
                output = Encoding.Default.GetString(ms.ToArray());
            }
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.Write(output);
        }
    }
}
