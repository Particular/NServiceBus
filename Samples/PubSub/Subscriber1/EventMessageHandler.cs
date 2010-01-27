using log4net;
using MyMessages;
using NServiceBus;

namespace Subscriber1
{
    public class EventMessageHandler : IHandleMessages<EventMessage>
    {
        public void Handle(EventMessage message)
        {
            Logger.Info(string.Format("Subscriber 1 received EventMessage with Id {0}.", message.EventId));
            Logger.Info(string.Format("Message time: {0}.", message.Time));
            Logger.Info(string.Format("Message duration: {0}.", message.Duration));
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EventMessageHandler));
    }
}
