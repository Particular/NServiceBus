using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using NServiceBus;

namespace WebService1
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            IBus bClient = builder.Build<IBus>();

            bClient.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}