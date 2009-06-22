using System;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast;
using Common.Logging;

namespace NServiceBus.Proxy
{
    public class Proxy
    {
        #region config

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

        public IProxyDataStorage Storage { private get; set; }

        public string RemoteServer
        {
            get { return remoteServer; }
            set
            {
                var arr = value.Split('@');

                var queue = arr[0];
                var machine = Environment.MachineName;

                if (arr.Length == 2)
                    if (arr[1] != "." && arr[1].ToLower() != "localhost")
                        machine = arr[1];

                remoteServer = queue + "@" + machine;
            }
        }

        private string remoteServer;

        #endregion


        public void Start()
        {
            internalTransport.Start();
            externalTransport.Start();
        }

        void ExternalTransportTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (e.Message.MessageIntent == MessageIntentEnum.Publish)
            {
                string val = null;
                foreach (var header in e.Message.Headers)
                    if (header.Key == UnicastBus.EnclosedMessageTypes)
                        val = header.Value;

                var types = UnicastBus.DeserializeEnclosedMessageTypes(val);

                var subs = Subscribers.GetSubscribersForMessage(types);

                Logger.Debug("Received notification from " + remoteServer + ".");

                foreach(var s in subs)
                    internalTransport.Send(e.Message, s);
            }
            else
            {
                ProxyData data = null;

                if (e.Message.CorrelationId != null)
                    data = Storage.GetAndRemove(e.Message.CorrelationId);

                if (data == null)
                    return;

                e.Message.CorrelationId = data.CorrelationId;

                Logger.Debug("Received response from " + remoteServer + ".");

                internalTransport.Send(e.Message, data.ClientAddress);
            }
        }

        void InternalTransportTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (UnicastBus.HandledSubscriptionMessage(e.Message, Subscribers))
            {
                e.Message.ReturnAddress = externalTransport.Address;
                externalTransport.Send(e.Message, remoteServer);

                Logger.Debug("Received subscription message.");
                return;
            }

            if (e.Message.Body != null)
                if (e.Message.Body[0] is CompletionMessage)
                    return;

            var data = new ProxyData
                           {
                               Id = GenerateId(),
                               ClientAddress = e.Message.ReturnAddress,
                               CorrelationId = e.Message.IdForCorrelation
                           };

            Storage.Save(data);

            Logger.Debug("Forwarding request to " + remoteServer + ".");

            e.Message.IdForCorrelation = data.Id;
            e.Message.ReturnAddress = externalTransport.Address;

            externalTransport.Send(e.Message, remoteServer);

            return;
        }

        private static string GenerateId()
        {
            return Guid.NewGuid() + "\\0";
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (Proxy));
    }
}
