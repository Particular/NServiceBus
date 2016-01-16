namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Messaging;

    interface IMsmqSubscriptionStorageQueue
    {
        IEnumerable<Message> GetAllMessages();
        void Send(Message toSend);
        void ReceiveById(string messageId);
    }
}