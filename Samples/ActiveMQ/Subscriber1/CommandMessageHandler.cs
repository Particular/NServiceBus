namespace Subscriber1
{
    using System;

    using MyMessages.Subscriber1;

    using NServiceBus;
    using NServiceBus.Logging;

    public class CommandMessageHandler : IHandleMessages<IMyRequest1>
    {
        public void Handle(IMyRequest1 message)
        {
            Logger.Info(string.Format("Subscriber 1 received IMyRequest1 with Id {0}.", message.EventId));
            Logger.Info(string.Format("Message time: {0}.", message.Time));
            Logger.Info(string.Format("Message duration: {0}.", message.Duration));
            Console.WriteLine("==========================================================================");
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandMessageHandler));
    }
}