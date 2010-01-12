using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Configuration;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Queuing.Msmq;
using NServiceBus.Unicast.Transport.Msmq;
using NServiceBus.Gateway;

namespace WebGateway
{
    public class Global : System.Web.HttpApplication
    {
        private static string inputQueue;
        private static string remoteUrl;
        private static string outputQueue;
        private static IMessageQueue messageQueue;

        protected void Application_Start(object sender, EventArgs e)
        {
            inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            outputQueue = ConfigurationManager.AppSettings["OutputQueue"];
            remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];

            messageQueue = new MsmqMessageQueue();
            messageQueue.Init(inputQueue);

            var transport = new MsmqTransport
                                {
                                    IsTransactional = true,
                                    NumberOfWorkerThreads = 1,
                                    MessageQueue = messageQueue
                                };

            transport.TransportMessageReceived += (s, args) =>
                MsmqHandler.Handle(args.Message, remoteUrl);

            transport.Start();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }        

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpRequestHandler.Handle(HttpContext.Current.AsIContext(), messageQueue, outputQueue);
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