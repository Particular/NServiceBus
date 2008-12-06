using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;
using NServiceBus.Messages;

namespace NServiceBus.Proxy
{
    public class Proxy
    {
        #region config

        private ISubscriberStorage subscribers;
        public virtual ISubscriberStorage Subscribers
        {
            set { subscribers = value; }
        }

        private ITransport externalTransport;
        public virtual ITransport ExternalTransport
        {
            set
            {
                this.externalTransport = value;

                this.externalTransport.TransportMessageReceived += externalTransport_TransportMessageReceived;
            }
        }

        private ITransport internalTransport;
        public virtual ITransport InternalTransport
        {
            set
            {
                this.internalTransport = value;
                this.internalTransport.MessageTypesToBeReceived =
                    new List<Type>(new Type[] { typeof(SubscriptionMessage), typeof(ReadyMessage) });

                this.internalTransport.TransportMessageReceived += internalTransport_TransportMessageReceived;
            }
        }
        private IProxyDataStorage storage;
        public virtual IProxyDataStorage Storage
        {
            set { this.storage = value; }
        }

        public virtual string RemoteServer
        {
            set
            {
                string[] arr = value.Split('@');

                string queue = arr[0];
                string machine = Environment.MachineName;

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
            this.internalTransport.Start();
            this.externalTransport.Start();
        }

        void externalTransport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            ProxyData data = null;
            
            if (e.Message.CorrelationId != null)
                data = this.storage.GetAndRemove(e.Message.CorrelationId);

            if (data == null)
            {
                if (HandledPublish(e.Message))
                    return;
            }
            else
            {
                // response from server
                e.Message.CorrelationId = data.CorrelationId;

                this.internalTransport.Send(e.Message, data.ClientAddress);
            }
        }

        void internalTransport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (HandledSubscription(e.Message))
                return;

            ProxyData data = new ProxyData();
            data.Id = GenerateId();
            data.ClientAddress = e.Message.ReturnAddress;
            data.CorrelationId = e.Message.IdForCorrelation;

            this.storage.Save(data);

            e.Message.IdForCorrelation = data.Id;
            e.Message.ReturnAddress = this.externalTransport.Address;

            this.externalTransport.Send(e.Message, remoteServer);

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

            foreach(string sub in subscribers.GetAllSubscribers())
                this.internalTransport.Send(message, sub);

            return true;
        }

        private bool HandledSubscription(TransportMessage transportMessage)
        {
            if (transportMessage.Body == null)
                return false;

            SubscriptionMessage sub = transportMessage.Body[0] as SubscriptionMessage;
            if (sub == null)
                return false;

            if (sub.SubscriptionType == SubscriptionType.Add)
                subscribers.Store(transportMessage.ReturnAddress);
            if (sub.SubscriptionType == SubscriptionType.Remove)
                subscribers.Remove(transportMessage.ReturnAddress);

            transportMessage.ReturnAddress = this.externalTransport.Address;
            this.externalTransport.Send(transportMessage, remoteServer);

            return true;
        }

        private static string GenerateId()
        {
            return Guid.NewGuid() + "\\0";
        }

    }
}
