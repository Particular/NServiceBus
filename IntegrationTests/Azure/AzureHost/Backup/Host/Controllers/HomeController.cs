using System.Web.Mvc;
using Messages;
using NServiceBus;

namespace Host.Controllers
{
    public class HomeController : Controller, IHandleMessages<Hello>
    {
        private readonly IBus bus;
        private static string text = "";

        public HomeController(IBus bus)
        {
            this.bus = bus;
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Hello()
        {
            text = "";

            bus.Send(new SayHelloTo {Name = Request["name"]});

            return View();
        }

        [HttpPost]
        public string Text()
        {
            return text;
        }

        public void Handle(Hello message)
        {
            text = message.Text;
        }
    }
}
