namespace NServiceBus.Unicast.Publishing
{
    using System;
    using NServiceBus.Transports;

    class StorageDrivenPublisherNonFunctionalPublisher : IPublishMessages
    {
        public void Publish(TransportMessage message, PublishOptions publishOptions)
        {
            throw new InvalidOperationException("Storage-driven publishing does not work in send-only endpoints because it requires to be able to process Subscriber messages and durably store the subscription information.");
        }
    }
}