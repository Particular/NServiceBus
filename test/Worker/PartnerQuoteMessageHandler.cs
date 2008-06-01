using System;
using Messages;
using NServiceBus;
using System.Threading;

namespace Worker
{
    public class PartnerQuoteMessageHandler : BaseMessageHandler<PartnerQuoteMessage>
    {
        public override void Handle(PartnerQuoteMessage message)
        {
            Thread.Sleep(TimeSpan.FromSeconds(new Random().Next(this.maxRandomSecondsToSleep)));

            message.Quote = 10 + new Random().Next(10) * 10;

            Console.WriteLine("Partner {0} provided quote {1}.", message.PartnerNumber, message.Quote);

            this.Bus.Reply(message);
        }

        private int maxRandomSecondsToSleep = 5;
        public virtual int MaxRandomSecondsToSleep
        {
            set { maxRandomSecondsToSleep = value; }
        }
    }
}
