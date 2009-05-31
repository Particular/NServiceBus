using System;
using System.Configuration;
using NServiceBus.Unicast.Transport.Msmq;
using System.Net;
using System.Threading;
using NServiceBus.Unicast.Transport;
using System.IO;
using NServiceBus.Gateway;

namespace Gateway.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputQueue = ConfigurationManager.AppSettings["InputQueue"];
            string outputQueue = ConfigurationManager.AppSettings["OutputQueue"];
            string listenUrl = ConfigurationManager.AppSettings["ListenUrl"];
            string remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];

            Action status = () =>
            {
                Console.WriteLine("Listening on queue: " + inputQueue);
                Console.WriteLine("Listening on url: " + listenUrl);
            };

            status();

            MsmqTransport transport = new MsmqTransport();
            transport.InputQueue = inputQueue;
            transport.IsTransactional = true;
            transport.SkipDeserialization = true;
            transport.NumberOfWorkerThreads = 1;

            transport.TransportMessageReceived += (s, e) =>
                {
                    MsmqHandler.Handle(e.Message, remoteUrl);
                    status();
                };

            transport.Start();

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenUrl);
            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                new Thread(new ParameterizedThreadStart(
                    (object o) =>
                        {
                            HttpRequestHandler.Handle(((HttpListenerContext)o).AsIContext(), transport, outputQueue);
                            status();
                        }
                    )).Start(context);
            }

            Console.WriteLine("Press 'Enter' to exit.");
            Console.ReadLine();


            listener.Close();
        }
    }
}
