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
            int number;
            if (!int.TryParse(textField, out number))
                return;
            
            var command = new Command { Id = number };
            Bus.Send(command).Register<int>(status=>
                                           {
                                               AsyncManager.Parameters["errorCode"] = Enum.GetName(typeof(ErrorCodes), status);
                                           });
        }

        public ActionResult IndexCompleted(string errorCode)
        {
            ViewBag.Title = "SendAsync"; 
            ViewBag.ResponseText = errorCode; 
            return View("Index");
        }
    }
}
