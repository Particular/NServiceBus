namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Unicast.Transport;

    public class MessageNotifier : IMessageNotifier
    {
        public event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded;
        
        
        void IMessageNotifier.RaiseMessageForwarded(Type from, Type to, TransportMessage message)
        {
            if (MessageForwarded != null)
                MessageForwarded(this, new MessageReceivedOnChannelArgs
                                           {
                                               FromChannel = from,
                                               ToChannel = to, 
                                               Message = message
                                           });
        }
    }
}