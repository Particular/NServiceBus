using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Gateway
{
    public interface INotifyAboutMessages
    {
        event EventHandler<MessageForwardingArgs> MessageForwarded;
    }

    public class MessageForwardingArgs : EventArgs
    {
        public TransportMessage Message { get; set; }
        public ChannelType FromChannel { get; set; }
        public ChannelType ToChannel { get; set; }
    }

    public interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageForwarded(ChannelType from,ChannelType to, TransportMessage message);
    }

    internal class MessageNotifier : IMessageNotifier
    {
        public event EventHandler<MessageForwardingArgs> MessageForwarded;
        
        
        void IMessageNotifier.RaiseMessageForwarded(ChannelType from, ChannelType to, TransportMessage message)
        {
            if (MessageForwarded != null)
                MessageForwarded(this, new MessageForwardingArgs
                                           {
                                               FromChannel = from,
                                               ToChannel = to, 
                                               Message = message
                                           });
        }
    }
}
