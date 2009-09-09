using System;
using System.ServiceModel;
using NServiceBus.Host;
using Server.WebServices;

namespace Server
{
    public class WcfServiceManager : IWantToRunAtStartup
    {
        private readonly ServiceHost serviceHost = new ServiceHost(typeof (CancelOrderService));

        public void Run()
        {
            serviceHost.Open();

            Console.WriteLine("The CancelOrder WCF service is ready.");
        }

        public void Stop()
        {
            serviceHost.Close();
        }
    }
}