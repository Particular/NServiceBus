using System;
using MyMessages;
using MyMessages.Other;
using NServiceBus;
using NServiceBus.Logging;

namespace Subscriber1
{
    using System.Transactions;

    public class AnotherEventMessageHandler : IHandleMessages<AnotherEventMessage>
    {
        private readonly IBus bus;

        public AnotherEventMessageHandler(IBus bus)
        {
            this.bus = bus;
        }

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