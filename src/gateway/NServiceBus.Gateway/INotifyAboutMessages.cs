using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Gateway
{
    public interface INotifyAboutMessages
    {
        event EventHandler<MessageTransportArgs> MessageProcessed;
    }

    public class MessageTransportArgs : EventArgs
    {
        public TransportTypeEnum TransportType { get; set; }
        public TransportMessage Message { get; set; }
    }

    public enum TransportTypeEnum { FromHttpToMsmq, FromMsmqToHttp }

    internal interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageProcessed(TransportTypeEnum transportType, TransportMessage message);
    }

    internal class MessageNotifier : IMessageNotifier
    {
        public event EventHandler<MessageTransportArgs> MessageProcessed;

        void IMessageNotifier.RaiseMessageProcessed(TransportTypeEnum transportType, TransportMessage message)
        {
            if (MessageProcessed != null)
                MessageProcessed(this, new MessageTransportArgs { TransportType = transportType, Message = message });
        }
    }
}
