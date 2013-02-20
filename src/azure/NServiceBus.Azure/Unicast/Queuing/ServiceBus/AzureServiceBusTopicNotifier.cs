using System;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    public class AzureServiceBusTopicNotifier : INotifyReceivedMessages
    {
        public void Start(Address address, Action<BrokeredMessage> tryProcessMessage)
        {

        }

        public void Stop()
        {
            
        }
    }
}