namespace NServiceBus.Unicast
{
    using System;

    internal class SendOptions
    {
        SendOptions()
        {
            Intent = MessageIntentEnum.Publish;

            if (!Configure.SendOnlyMode)
            {
                ReplyToAddress = Address.Local;
            }
        }
        public SendOptions(Address destination):this()
        {
            Intent = MessageIntentEnum.Send;
            this.destination = destination;
            
        }

        public SendOptions(string destination): this(Address.Parse(destination))
        {
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

        public Address ReplyToAddress { get; set; }

        public static SendOptions Publish
        {
            get
            {
                return new SendOptions();
            }
        }

        public DateTime? ProcessAt { get; set; }


        public static SendOptions ReplyTo(Address replyToAddress)
        {
            if (replyToAddress == null)
                throw new InvalidOperationException("Can't reply with null reply-to-address field. It can happen if you are using a SendOnly client. See http://particular.net/articles/one-way-send-only-endpoints");

            return new SendOptions(replyToAddress){Intent = MessageIntentEnum.Reply};
        }

        readonly Address destination;

    }
}