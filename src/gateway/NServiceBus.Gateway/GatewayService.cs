namespace NServiceBus.Gateway
{
    using System.Configuration;
    using System.Net;
    using System.Threading;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class GatewayService : IWantToRunAtStartup
    {
        public GatewayService(ITransport transport, ISendMessages messageSender, IMessageNotifier notifier, HttpListener listener)
        {
            this.transport = transport;
            this.listener = listener;
            this.notifier = notifier;
            this.messageSender = messageSender;
        }

        public void Run()
        {
            var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            var outputQueue = ConfigurationManager.AppSettings["OutputQueue"];

            transport.Start(inputQueue);
            listener.Start();

            while (!stopRequested)
            {
                ThreadPool.QueueUserWorkItem(
                    o => new HttpReceiver(messageSender, 
                                                notifier, 
                                                inputQueue, 
                                                outputQueue, 
                                                new Persistence { ConnectionString = connectionString }).Handle(o as HttpListenerContext),
                    listener.GetContext());
            }
        }

        public void Stop()
        {
            stopRequested = true;
        }
        
        volatile bool stopRequested;

        readonly ITransport transport;
        readonly ISendMessages messageSender;
        readonly IMessageNotifier notifier;
        readonly HttpListener listener;


    }
}