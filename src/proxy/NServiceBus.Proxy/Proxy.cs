namespace NServiceBus.Proxy
{
    using System;
    using Logging;
    using Unicast;
    using Unicast.Queuing;
    using Unicast.Subscriptions;
    using Unicast.Transport;
    using System.Linq;
    using Unicast.Monitoring;
    using MessageType = Unicast.Subscriptions.MessageType;

    public class Proxy
    {
        public ISubscriptionStorage Subscribers { get; set; }

        private ITransport externalTransport;
        public ITransport ExternalTransport
        {
            set
            {
                externalTransport = value;

                externalTransport.TransportMessageReceived += ExternalTransportTransportMessageReceived;
            }
        }

        private ITransport internalTransport;
        public ITransport InternalTransport
        {
            set
            {
                internalTransport = value;

                internalTransport.TransportMessageReceived += InternalTransportTransportMessageReceived;
            }
        }

        public ISendMessages InternalMessageSender { get; set; }

        public ISendMessages ExternalMessageSender { get; set; }

        public IProxyDataStorage Storage { private get; set; }

        public Address RemoteServer{ get; set; }

        public Address ExternalAddress { get; set; }

        public Address InternalAddress { get; set; }

      
        public void Start()
        {
            Storage.Init();
            internalTransport.Start(InternalAddress);
            externalTransport.Start(ExternalAddress);
        }

        void ExternalTransportTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (e.Message.MessageIntent == MessageIntentEnum.Publish)
            {
                if(!e.Message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
                    throw new InvalidOperationException("Enclosed message type header was not found in message");


                var types = e.Message.Headers[Headers.EnclosedMessageTypes].Split(';');

                var subs = Subscribers.GetSubscriberAddressesForMessage(types.Select(s=> new MessageType(s)));

                Logger.Debug("Received notification from " + RemoteServer + ".");

                foreach(var s in subs)
                    InternalMessageSender.Send(e.Message, s);
            }
            else
            {
                ProxyData data = null;

                if (e.Message.CorrelationId != null)
                    data = Storage.GetAndRemove(e.Message.CorrelationId);

                if (data == null)
                    return;

                e.Message.CorrelationId = data.CorrelationId;

                Logger.Debug("Received response from " + RemoteServer + ".");

                InternalMessageSender.Send(e.Message, data.ClientAddress);
            }
        }

        void InternalTransportTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (UnicastBus.HandledSubscriptionMessage(e.Message, Subscribers, null))
            {
                e.Message.ReplyToAddress = ExternalAddress;
                ExternalMessageSender.Send(e.Message, RemoteServer);

                Logger.Debug("Received subscription message.");
                return;
            }

            var data = new ProxyData
                           {
                               Id = GenerateId(),
                               ClientAddress = e.Message.ReplyToAddress,
                               CorrelationId = e.Message.IdForCorrelation
                           };

            Storage.Save(data);

            Logger.Debug("Forwarding request to " + RemoteServer + ".");

            e.Message.IdForCorrelation = data.Id;
            e.Message.ReplyToAddress = ExternalAddress;

            ExternalMessageSender.Send(e.Message, RemoteServer);
        }

        static string GenerateId()
        {
            return Guid.NewGuid() + "\\0";
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof (Proxy));
    }
}
