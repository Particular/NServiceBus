using System;
using NServiceBus;

namespace WebService1
{
    public class Global : System.Web.HttpApplication
    {
        public static IBus Bus { get; private set; }

        protected void Application_Start(object sender, EventArgs e)
        {
            Bus = Configure.WithWeb()
                .Log4Net()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                .UnicastBus()
                    .LoadMessageHandlers()
                .CreateBus()
                .Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}