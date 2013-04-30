using System;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Unicast.Queuing.Windows.ServiceBus
{
    public interface INotifyReceivedMessages
    {
        void Start(Address address, Action<BrokeredMessage> tryProcessMessage);
        void Stop();
    }
}