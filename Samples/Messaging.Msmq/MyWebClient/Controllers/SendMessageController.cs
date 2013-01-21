using System.Web.Mvc;

namespace MyWebClient.Controllers
{
    using System;
    using System.Threading.Tasks;
    using MyMessages.Commands;
    using NServiceBus;

    public class SendMessageController : Controller
    {
        public IBus Bus { get; set; }

        public async Task<ActionResult> Index(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "Anonymous";
            }
            // We use the support for asynchronous methods in MVC 4 to avoid holding on precious web server threads
            // while the server is processing our command - http://www.asp.net/mvc/tutorials/mvc-4/using-asynchronous-methods-in-aspnet-mvc-4
            // Please see AsyncController if you're using an older version of MVC - http://msdn.microsoft.com/en-us/library/ee728598(v=vs.100).aspx
            var status =  await Bus.Send(new MyCommand { Name = text })
                .Register<CommandStatus>();

            return new JsonResult
            {
                Data = new
                {
                    Message = String.Format("Hello {0}! ", text),
                    ResponseStatus = status
                }
            };
        }

    }
}
