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

            Guid result = Guid.NewGuid();
            this.bus.Reply<ResponseToPublisher>(m =>
                {
                    m.ResponseId = result;
                    m.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
                    m.Duration = TimeSpan.FromSeconds(99999D);
                });
            Console.WriteLine("Replied with response {0}", result);

            if (message.ThrowExceptionDuringProcessing)
            {
                Console.WriteLine("Throwing Exception");
                throw new Exception();
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandMessageHandler));
    }
}