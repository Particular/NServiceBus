namespace MyServer.PerformanceTest
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using NServiceBus;

    public class PerformanceTestMessageHandler : IHandleMessages<PerformanceTestMessage>
    {
        public static ConcurrentBag<string> receivedMessages = new ConcurrentBag<string>();

        public static int NumExpectedMessages;
        public static DateTime TimeStarted;
        public static DateTime TimeEnded;

        public IBus Bus { get; set; }

        public void Handle(PerformanceTestMessage message)
        {
            receivedMessages.Add(Bus.CurrentMessageContext.Id);

            Console.WriteLine(string.Format("Message {0}({1})", receivedMessages.Count, NumExpectedMessages));

            if (NumExpectedMessages == receivedMessages.Count)
            {
                TimeEnded = DateTime.UtcNow;
                Console.WriteLine(string.Format("Test finished, total time: {0}", TimeEnded - TimeStarted));
            }

            if (receivedMessages.Count > NumExpectedMessages && NumExpectedMessages > 0)
                throw new InvalidOperationException("More messages than expected received");
        }
    }
}