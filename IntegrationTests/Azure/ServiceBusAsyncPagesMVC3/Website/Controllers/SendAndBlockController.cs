using System;
using System.Threading;
using System.Web.Mvc;
using Contract;
using NServiceBus;

namespace Website
{
    public class SendAndBlockController : Controller
    {
        // Bus property to be injected
        public IBus Bus { get; set; }
        
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Title = "SendAndBlock";
            return View();
        }

        [HttpPost]
        public ActionResult Index(string textField)
        {
            ViewBag.Title = "SendAndBlock";

            int number;
            if (!int.TryParse(textField, out number))
                return View();

            var command = new Command { Id = number };

            IAsyncResult res = Bus.Send(command).Register(SimpleCommandCallback, this);
            WaitHandle asyncWaitHandle = res.AsyncWaitHandle;
            asyncWaitHandle.WaitOne(50000);
            
            return View();
        }
        
        private void SimpleCommandCallback(IAsyncResult asyncResult)
        {
            var result = asyncResult.AsyncState as CompletionResult;
            var controller = result.State as SendAndBlockController;
            controller.ViewBag.ResponseText = Enum.GetName(typeof (ErrorCodes), result.ErrorCode);
        }

    }
}
