using System;
using System.Collections.Generic;
using System.Web;
using MyMessages;
using NServiceBus;
using NServiceBus.Features;

namespace OrderWebSite
{
    using System.Configuration;
    using NServiceBus.Unicast.Queuing;

    public class Global : HttpApplication
    {
		public static IBus Bus;
		public static IList<MyMessages.Order> Orders;
    	
		private static readonly Lazy<IBus> StartBus = new Lazy<IBus>(ConfigureNServiceBus);
		
		private static IBus ConfigureNServiceBus()
		{
		    Feature.Disable<Gateway>();
            Feature.Disable<SecondLevelRetries>();
            Feature.Disable<TimeoutManager>();
            
            var bus = Configure.With()
                .DefaultBuilder()
                .AzureConfigurationSource()
                .AzureMessageQueue()
                    .QueuePerInstance()
                .UnicastBus()
                .CreateBus()
				.Start();

		    try
		    {
                bus.Send(new LoadOrdersMessage());
		    }
		    catch (ConfigurationErrorsException ex)
		    {
		        //swallow a q not found since there is a race condition when starting the sample on a clean box
		        if (!(ex.InnerException is QueueNotFoundException))
		            throw;
		    }
			

			return bus;
		}

    	protected void Application_Start(object sender, EventArgs e)
        {
			Orders = new List<MyMessages.Order>();
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			Bus = StartBus.Value;
		}

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
           
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}