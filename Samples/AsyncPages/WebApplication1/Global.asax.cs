using System;
using System.Web;
using NServiceBus;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;

namespace WebApplication1
{
    public class Global : HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
            //NServiceBus.Serializers.Configure.XmlSerializer.With(builder);

            new ConfigMsmqTransport(builder)
                .IsTransactional(false)
                .PurgeOnStartup(false);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false);

            IBus bus = builder.Build<IBus>();
            bus.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}