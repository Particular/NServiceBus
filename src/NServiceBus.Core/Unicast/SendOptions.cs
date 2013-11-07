namespace NServiceBus.Unicast
{
    using System;

    internal class SendOptions
    {
        public SendOptions(Address destination)
        {
            this.destination = destination;
            Intent = MessageIntentEnum.Send;
        }

        public SendOptions(string destination)
        {
            this.destination = Address.Parse(destination);
        }

        public MessageIntentEnum Intent { get; set; }
        public Address Destination { get { return destination; } }
        public string CorrelationId { get; set; }

        public static SendOptions ToLocalEndpoint
        {
            get
            {
                return new SendOptions(Address.Local);
            }
        }



        public static SendOptions ReplyTo(Address replyToAddress)
        {
            if (replyToAddress == null)
                throw new InvalidOperationException("Can't reply with null reply-to-address field. It can happen if you are using a SendOnly client. See http://particular.net/articles/one-way-send-only-endpoints");

            return new SendOptions(replyToAddress){Intent = MessageIntentEnum.Reply};
        }

        readonly Address destination;

    }
}