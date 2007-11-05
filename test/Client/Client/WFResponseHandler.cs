using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using NServiceBus;

namespace Client
{
    public class WFResponseHandler : BaseMessageHandler<PriceQuoteResponse>
    {
        public override void Handle(PriceQuoteResponse message)
        {
            Console.WriteLine("Best quote: " + message.Quote.ToString());
        }
    }
}
