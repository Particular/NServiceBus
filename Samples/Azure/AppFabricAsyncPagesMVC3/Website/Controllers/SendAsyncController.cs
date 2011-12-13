using System;
using System.Web.Mvc;
using Contract;
using NServiceBus;

namespace Website
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
            int number;
            if (!int.TryParse(textField, out number))
                return;
            
            AsyncManager.OutstandingOperations.Increment();
            var command = new Command { Id = number };
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
