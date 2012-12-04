using System.Web.Mvc;

namespace MyWebClient.Controllers
{
    using MyMessages;
    using NServiceBus;

    public class SendMessageController : Controller
    {
        public IBus Bus { get; set; }

        public ActionResult Index()
        {
            Bus.Send(new MyCommand{ Description = "This message was sent from MyWebClient"});

            return new ContentResult{ Content = "Message of type MyCommand sent to MyServer"};
        }

    }
}
