using System;
using MyMessages.Other;
using NServiceBus;
using NServiceBus.Logging;

namespace Subscriber1
{
    public class AnotherEventMessageHandler : IHandleMessages<AnotherEventMessage>
    {
        public void Handle(AnotherEventMessage message)
        {
            Logger.Info(string.Format("Subscriber 1 received AnotherEventMessage with Id {0}.", message.EventId));
            Logger.Info(string.Format("Message time: {0}.", message.Time));
            Logger.Info(string.Format("Message duration: {0}.", message.Duration));
            Console.WriteLine("==========================================================================");
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (AnotherEventMessageHandler));
    }
}