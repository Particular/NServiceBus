using System;
using Messages;
using NServiceBus;

namespace Client
{
    public class PriceQuoteResponseHandler : BaseMessageHandler<PriceQuoteResponse>
    {
        public override void Handle(PriceQuoteResponse message)
        {
            Console.WriteLine(string.Format("Best quote: {0}.", message.Quote));
        }
    }
}
