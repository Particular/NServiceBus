using System;
using System.Diagnostics;
using NServiceBus;
using MyMessages;

namespace MyClient
{
    public class ClientEndpoint : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            if (!Environment.UserInteractive) SendRequestDataMessage();

            Console.WriteLine("Press 'Enter' to send a message.To exit, Ctrl + C");

            while (Console.ReadLine() != null)
            {
                SendRequestDataMessage();
            }
        }

        public void Stop()
        {
        }

        private void SendRequestDataMessage()
        {
            var g = Guid.NewGuid();

            Console.WriteLine("==========================================================================");
            Console.WriteLine("Requesting to get data by id: {0}", g.ToString("N"));

            Bus.OutgoingHeaders["Test"] = g.ToString("N");

            var watch = new Stopwatch();
            watch.Start();
            Bus.Send<RequestDataMessage>(m =>
            {
                m.DataId = g;
                m.String = "<node>it's my \"node\" & i like it<node>";
            })
                .Register<int>(i =>
                {
                    Console.WriteLine("==========================================================================");
                    Console.WriteLine(
                        "Response with header 'Test' = {0}, 1 = {1}, 2 = {2}.",
                        Bus.CurrentMessageContext.Headers["Test"],
                        Bus.CurrentMessageContext.Headers["1"],
                        Bus.CurrentMessageContext.Headers["2"]);
                });

            watch.Stop();

            Console.WriteLine("Elapsed time: {0}", watch.ElapsedMilliseconds);
        }
    }
}
