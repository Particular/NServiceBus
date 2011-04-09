namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Channels;
    using NServiceBus.Unicast.Transport;

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