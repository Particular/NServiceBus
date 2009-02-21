using System;
using NServiceBus;

namespace WebService1
{
    public class Global : System.Web.HttpApplication
    {
        public static IBus Bus { get; private set; }

        protected void Application_Start(object sender, EventArgs e)
        {
            Bus = NServiceBus.Configure.WithWeb()
                .SpringBuilder()
                .BinarySerializer()
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