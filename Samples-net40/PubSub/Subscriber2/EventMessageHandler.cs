using log4net;
using MyMessages;
using NServiceBus;

namespace Subscriber2
{
    public class EventMessageHandler : IHandleMessages<IEvent>
    {
        public void Handle(IEvent message)
        {
            Logger.Info(string.Format("Subscriber 2 received IEvent with Id {0}.", message.EventId));
            Logger.Info(string.Format("Message time: {0}.", message.Time));
            Logger.Info(string.Format("Message duration: {0}.", message.Duration));
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (EventMessageHandler));
    }
}
