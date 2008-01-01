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

            ServiceEndpoint endpoint = this.host.AddServiceEndpoint(typeof(IOneWay), binding, address);

            this.address = endpoint.Address.Uri.AbsoluteUri;
        }

        void service_MessageReceived(object sender, MessageEventArgs e)
        {
            if (this.MsgReceived != null)
                this.MsgReceived(this, new MsgReceivedEventArgs(this.Convert(e.Message)));
        }

        #region ITransport Members

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
                    this.serializer = ci.Invoke(new object[] { typeof(Msg), value }) as XmlObjectSerializer;
            }
        }

        public event EventHandler<MsgReceivedEventArgs> MsgReceived;

        public void Start()
        {
            this.host.Open();
        }

        public void Send(Msg m, string destination)
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

        public void ReceiveMessageLater(Msg m)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.host.Close();
        }

        #endregion

        #region helper methods

        private Message Convert(Msg msg)
        {
            object body = msg.Body;

            if (binding.MessageVersion.Addressing == AddressingVersion.None)
                body = msg;

            Message wcfMessage = Message.CreateMessage(binding.MessageVersion, "*", body, this.serializer);

            if (binding.MessageVersion.Addressing != AddressingVersion.None)
            {
                wcfMessage.Headers.ReplyTo = new EndpointAddress(new Uri(this.address));
                wcfMessage.Headers.MessageId = new System.Xml.UniqueId(Guid.NewGuid());

                if (msg.CorrelationId != null)
                    wcfMessage.Headers.RelatesTo = new System.Xml.UniqueId(msg.CorrelationId);
            }

            return wcfMessage;
        }

        private Msg Convert(Message m)
        {
            Msg msg = new Msg();

            if (binding.MessageVersion.Addressing == AddressingVersion.None)
                msg = m.GetBody<Msg>(this.serializer);
            else
            {
                msg.Body = m.GetBody<IMessage[]>(this.serializer);

                msg.ReturnAddress = m.Headers.ReplyTo.Uri.AbsoluteUri;
                msg.Id = m.Headers.MessageId.ToString();

                if (m.Headers.RelatesTo != null)
                    msg.CorrelationId = m.Headers.RelatesTo.ToString();
            }

            return msg;
        }

        #endregion

        #region members

        private readonly ServiceHost host;
        private readonly IChannelFactory<IOneWay> channelFactory;
        private readonly Binding binding;
        private XmlObjectSerializer serializer;
        private readonly Type xmlObjectSerializerType;

        private static ILog logger = LogManager.GetLogger(typeof(GenericTransport));

        #endregion
    }
}
