namespace Subscriber2
{
    using System;

    using MyMessages.Publisher;
    using MyMessages.Subscriber2;

    using NServiceBus;
    using NServiceBus.Logging;

    public class CommandMessageHandler : IHandleMessages<IMyRequest2>
    {
        private readonly IBus bus;

        public CommandMessageHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(IMyRequest2 message)
        {
            Logger.Info(string.Format("Subscriber 1 received IMyRequest2 with Id {0}.", message.CommandId));
            Logger.Info(string.Format("Message time: {0}.", message.Time));
            Logger.Info(string.Format("Message duration: {0}.", message.Duration));
            Console.WriteLine("==========================================================================");

            this.bus.Reply<ResponseToPublisher>(m =>
                {
                    m.ResponseId = Guid.NewGuid();
                    m.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
                    m.Duration = TimeSpan.FromSeconds(99999D);
                });
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandMessageHandler));
    }
}