using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;
using NServiceBus.Messages;

namespace NServiceBus.Proxy
{
    public class Proxy
    {
        #region config

        public ISubscriberStorage Subscribers { private get; set; }

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
            ProxyData data = null;
            
            if (e.Message.CorrelationId != null)
                data = Storage.GetAndRemove(e.Message.CorrelationId);

            if (data == null)
            {
                if (HandledPublish(e.Message))
                    return;
            }
            else
            {
                // response from server
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


        /// <summary>
        /// Assumes that no data could be found using correlation id.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool HandledPublish(TransportMessage message)
        {
            if (message.ReturnAddress != remoteServer)
                return false;

            foreach(var sub in Subscribers.GetAllSubscribers())
                internalTransport.Send(message, sub);

            return true;
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
