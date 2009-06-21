using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;
using NServiceBus.Messages;

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
                internalTransport.MessageTypesToBeReceived =
                    new List<Type>(new[] { typeof(SubscriptionMessage), typeof(ReadyMessage) });

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
                foreach (var sub in Subscribers.GetSubscribersForMessage())
                    internalTransport.Send(message, sub);

            }
            else
            {
                ProxyData data = null;

                if (e.Message.CorrelationId != null)
                    data = Storage.GetAndRemove(e.Message.CorrelationId);

                if (data == null)
                    return;

                e.Message.CorrelationId = data.CorrelationId;

                internalTransport.Send(e.Message, data.ClientAddress);
            }
        }

        void InternalTransportTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (HandledSubscription(e.Message))
                return;

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

            e.Message.IdForCorrelation = data.Id;
            e.Message.ReturnAddress = externalTransport.Address;

            externalTransport.Send(e.Message, remoteServer);

            return;
        }

        private bool HandledSubscription(TransportMessage transportMessage)
        {
            if (transportMessage.Body == null)
                return false;

            var sub = transportMessage.Body[0] as SubscriptionMessage;
            if (sub == null)
                return false;

            if (sub.SubscriptionType == SubscriptionType.Add)
                Subscribers.Store(transportMessage.ReturnAddress);
            if (sub.SubscriptionType == SubscriptionType.Remove)
                Subscribers.Remove(transportMessage.ReturnAddress);

            transportMessage.ReturnAddress = externalTransport.Address;
            externalTransport.Send(transportMessage, remoteServer);

            return true;
        }

        private static string GenerateId()
        {
            return Guid.NewGuid() + "\\0";
        }

    }
}
