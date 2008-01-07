using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using Common.Logging;

namespace NServiceBus.Unicast.Transport.WCF
{
    public class GenericTransport : ITransport
    {
        public GenericTransport(Binding binding, string baseAddress, string address, string xmlObjectSerializer)
        {
            xmlObjectSerializerType = Type.GetType(xmlObjectSerializer, true);

            ConstructorInfo ci = xmlObjectSerializerType.GetConstructor(Type.EmptyTypes);

            if (ci != null)
                this.serializer = Activator.CreateInstance(xmlObjectSerializerType) as XmlObjectSerializer;

            this.binding = binding;
            this.channelFactory = new ChannelFactory<IOneWay>(binding);

            OneWayService.MessageReceived += service_MessageReceived;

            this.host = new ServiceHost(typeof(OneWayService), new Uri(baseAddress));

            this.endpoint = this.host.AddServiceEndpoint(typeof(IOneWay), binding, address);

            this.address = endpoint.Address.Uri.AbsoluteUri;
        }

        void service_MessageReceived(object sender, MessageEventArgs e)
        {
            if (this.TransportMessageReceived != null)
                this.TransportMessageReceived(this, new TransportMessageReceivedEventArgs(this.Convert(e.Message)));
        }

        #region ITransport Members

        public int NumberOfWorkerThreads
        {
            get
            {
                //TODO: Implement this using ServiceThrottlingBehavior 
                return 1;
            }
        }

        public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
        {
            //TODO: Implement this using ServiceThrottlingBehavior 
        }

        public void StopSendingReadyMessages()
        {
            //TODO: Implement this
        }

        public void ContinueSendingReadyMessages()
        {
            //TODO: Implement this
        }

        public int GetNumberOfPendingMessages()
        {
            //TODO: Implement this
            throw new NotImplementedException();
        }

        private readonly string address;
        public string Address
        {
            get
            {
                return this.address;
            }
        }

        public IList<Type> MessageTypesToBeReceived
        {
            set
            {
                ConstructorInfo ci = this.xmlObjectSerializerType.GetConstructor(new Type[] { typeof(Type), typeof(IEnumerable<Type>) });
                if (ci != null)
                    this.serializer = ci.Invoke(new object[] { typeof(TransportMessage), value }) as XmlObjectSerializer;
            }
        }

        public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;

        public void Start()
        {
            this.host.Open();
        }

        public void Send(TransportMessage m, string destination)
        {
            ICommunicationObject comm = this.channelFactory;
            if (comm.State != CommunicationState.Opened)
                comm.Open();

            IOneWay sendChannel = this.channelFactory.CreateChannel(new EndpointAddress(destination));

            ((ICommunicationObject)sendChannel).Open();

            Message toSend = this.Convert(m);
            sendChannel.Process(toSend);

            m.Id = toSend.Headers.MessageId.ToString();

            ((ICommunicationObject) sendChannel).BeginClose(null, null);
        }

        public void ReceiveMessageLater(TransportMessage m)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.host.Close();
        }

        #endregion

        #region helper methods

        private Message Convert(TransportMessage TransportMessage)
        {
            object body = TransportMessage.Body;

            if (binding.MessageVersion.Addressing == AddressingVersion.None)
                body = TransportMessage;

            Message wcfMessage = Message.CreateMessage(binding.MessageVersion, "*", body, this.serializer);

            if (binding.MessageVersion.Addressing != AddressingVersion.None)
            {
                wcfMessage.Headers.ReplyTo = new EndpointAddress(new Uri(this.address));
                wcfMessage.Headers.MessageId = new System.Xml.UniqueId(Guid.NewGuid());

                if (TransportMessage.CorrelationId != null)
                    wcfMessage.Headers.RelatesTo = new System.Xml.UniqueId(TransportMessage.CorrelationId);
            }

            return wcfMessage;
        }

        private TransportMessage Convert(Message m)
        {
            TransportMessage TransportMessage = new TransportMessage();

            if (binding.MessageVersion.Addressing == AddressingVersion.None)
                TransportMessage = m.GetBody<TransportMessage>(this.serializer);
            else
            {
                TransportMessage.Body = m.GetBody<IMessage[]>(this.serializer);

                TransportMessage.ReturnAddress = m.Headers.ReplyTo.Uri.AbsoluteUri;
                TransportMessage.Id = m.Headers.MessageId.ToString();

                if (m.Headers.RelatesTo != null)
                    TransportMessage.CorrelationId = m.Headers.RelatesTo.ToString();
            }

            return TransportMessage;
        }

        #endregion

        #region members

        private readonly ServiceHost host;
        private readonly ServiceEndpoint endpoint;
        private readonly IChannelFactory<IOneWay> channelFactory;
        private readonly Binding binding;
        private XmlObjectSerializer serializer;
        private readonly Type xmlObjectSerializerType;

        private static ILog logger = LogManager.GetLogger(typeof(GenericTransport));

        #endregion
    }
}
