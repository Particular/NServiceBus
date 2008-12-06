using System;
using System.Web;
using NServiceBus;
using NServiceBus.Config;

namespace WebApplication1
{
    public class Global : HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            NServiceBus.Config.Configure.With(builder)
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);

            var bus = builder.Build<IStartableBus>();
            bus.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}