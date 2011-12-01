using System;
using System.Web.Mvc;
using Messages;
using NServiceBus;

namespace AsyncPagesMVC3.Controllers
{
    public class SendAsyncController : AsyncController
    {
        // Bus property to be injected
        public IBus Bus { get; set; }

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Title = "SendAsync";
            return View("Index");
        }
        
        [HttpPost]
        [AsyncTimeout(50000)]
        public void IndexAsync(string textField)
        {
            AsyncManager.OutstandingOperations.Increment();
            var command = new Command { Id = int.Parse(textField) };
            Bus.Send(command).Register(SimpleCommandCallback, this);
        }

        public ActionResult IndexCompleted(string errorCode)
        {
            ViewBag.Title = "SendAsync"; 
            ViewBag.ResponseText = errorCode; 
            return View("Index");
        }

        private void SimpleCommandCallback(IAsyncResult asyncResult)
        {
            var result = asyncResult.AsyncState as CompletionResult;
            AsyncManager.Parameters["errorCode"] = Enum.GetName(typeof(ErrorCodes), result.ErrorCode);
            AsyncManager.OutstandingOperations.Decrement();
        }
    }
}
