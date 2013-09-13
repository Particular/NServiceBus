using System;
using System.Diagnostics;
using MyMessages;
using NServiceBus;

namespace MyClient
{
    public class TestMessageHandler : IHandleMessages<TestStarterMessage>
    {
        public IBus Bus { get; set; }
        
        public void Handle(TestStarterMessage message)
        {
            Guid dataId = message.Id;

            Console.WriteLine("==========================================================================");
            Console.WriteLine("Requesting to get data by id: {0}", dataId.ToString("N"));

            Bus.OutgoingHeaders["Test"] = dataId.ToString("N");

            var watch = new Stopwatch();

            watch.Start();

            Bus.Send<RequestDataMessage>(m =>
            {
                m.DataId = dataId;
                m.String = "<node>it's my \"node\" & i like it<node>";
            })
            .Register<int>(i =>
                {
                    Console.WriteLine("==========================================================================");
                    Console.WriteLine("Response with header 'Test' = {0}, 1 = {1}, 2 = {2}.",
                        Bus.CurrentMessageContext.Headers["Test"],
                        Bus.CurrentMessageContext.Headers["1"],
                        Bus.CurrentMessageContext.Headers["2"]);
                });

            watch.Stop();

            Console.WriteLine("Elapsed time: {0}", watch.ElapsedMilliseconds);
        }
    }
}
