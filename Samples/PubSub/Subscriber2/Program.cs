using System;
using Common.Logging;
using Messages;
using NServiceBus;

namespace Subscriber2
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();
                
            IBus bClient = builder.Build<IBus>();

            bClient.Start();

            bClient.Subscribe(typeof(EventMessage));

            Console.WriteLine("Listening for events. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
            }
        }
    }
}
