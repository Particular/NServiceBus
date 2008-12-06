using System;
using NServiceBus;
using NServiceBus.Config;
using ObjectBuilder;

namespace WebService1
{
    public class Global : System.Web.HttpApplication
    {
        public static IBus Bus;
        public static IBuilder Builder;

        protected void Application_Start(object sender, EventArgs e)
        {
            Builder = new ObjectBuilder.SpringFramework.Builder();

            NServiceBus.Config.Configure.With(Builder)
                .BinarySerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);

            Bus = Builder.Build<IBus>();

            Builder.Build<IStartableBus>().Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {
            Builder.Build<IStartableBus>().Dispose();
        }
    }
}