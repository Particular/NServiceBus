using System;
using System.Web;
using NServiceBus;

namespace WebApplication1
{
    public class Global : HttpApplication
    {
        public static IBus Bus { get; private set; }

        protected void Application_Start(object sender, EventArgs e)
        {
            Bus = NServiceBus.Configure.WithWeb()
                .Log4Net()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                .CreateBus()
                .Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}