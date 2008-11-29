using System;
using NServiceBus;
using NServiceBus.Config;

namespace WebService1
{
    public class Global : System.Web.HttpApplication
    {
        public static IBus Bus;

        protected void Application_Start(object sender, EventArgs e)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            NServiceBus.Config.Configure.With(builder)
                .BinarySerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);

            Bus = builder.Build<IBus>();

            Bus.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {
            Bus.Dispose();
        }
    }
}