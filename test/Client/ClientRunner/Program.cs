using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;
using Messages;
using NServiceBus;
using NServiceBus.Async;
using System.Configuration;
using Common.Logging;

namespace ClientRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogManager.GetLogger("hello").Debug("Started.");
                ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

                string sendWF = System.Configuration.ConfigurationManager.AppSettings["SendWF"];
                
                IBus bClient = builder.Build<IBus>();

                bClient.Start();

                bClient.Subscribe(typeof(Event));

                int toSend = 1;
                Console.WriteLine("Press 'Enter' to send a message. To exit, press 'q' and then 'Enter'.");
                while (Console.ReadLine().ToLower() != "q")
                {
                    Messages.Command m = new Messages.Command();
                    m.i = toSend;

                    toSend++;

                    bClient.Send(m);

                    Console.WriteLine("{0}.{1}  Sent command: {2}", DateTime.Now.Second, DateTime.Now.Millisecond, m.i);

                    if (sendWF == "true")
                    {
                        bClient.Send(new PriceQuoteRequest());

                        Console.WriteLine("{0}.{1}  Sent WF: {2}", DateTime.Now.Second, DateTime.Now.Millisecond, m.i);
                    }
                }

                bClient.Unsubscribe(typeof(Event));
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Error("Fatal", e);
                Console.Read();
            }
        }
    }
}
