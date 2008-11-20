using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Proxy
{
    public class Proxy
    {
        #region config

        private ITransport transport;
        public virtual ITransport Transport
        {
            set
            {
                this.transport = value;
                this.transport.TransportMessageReceived += transport_TransportMessageReceived;
            }
        }

        private IProxyDataStorage storage;
        public virtual IProxyDataStorage Storage
        {
            set { this.storage = value; }
        }

        public virtual string RemoteServer
        {
            set { remoteServer = value; }
        }

        private string remoteServer;

        #endregion


        public void Start()
        {
            this.transport.Start();
        }

        void transport_TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            ProxyData data = null;
            
            if (e.Message.CorrelationId != null)
                data = this.storage.GetAndRemove(e.Message.CorrelationId);

            if (data == null) // first message from client
            {
                data = new ProxyData();
                data.Id = GenerateId();
                data.ClientAddress = e.Message.ReturnAddress;
                data.CorrelationId = e.Message.IdForCorrelation;

                this.storage.Save(data);

                e.Message.IdForCorrelation = data.Id;
                e.Message.ReturnAddress = this.transport.Address;

                this.transport.Send(e.Message, remoteServer);

                return;
            }

            // response from server
            e.Message.CorrelationId = data.CorrelationId;
            e.Message.ReturnAddress = this.transport.Address;

            this.transport.Send(e.Message, data.ClientAddress);
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString() + "\\0";
        }

    }
}
