using System;
using NServiceBus;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;

namespace WebService1
{
    public class Global : System.Web.HttpApplication
    {
        public static IBus Bus;

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

            Bus = builder.Build<IBus>();

            Bus.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}