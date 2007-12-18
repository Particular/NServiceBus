using System;
using System.Web;
using NServiceBus;

namespace WebApplication1
{
    public class Global : HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            IBus bus = builder.Build<IBus>();
            bus.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}