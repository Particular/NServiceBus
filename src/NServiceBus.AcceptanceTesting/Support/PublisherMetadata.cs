namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Metadata on events and their publishers.
    /// </summary>
    public class PublisherMetadata
    {
        public IEnumerable<PublisherDetails> Publishers => publisherDetails.Values;

        public void RegisterPublisherFor<T>(Type endpoint)
        {
            PublisherDetails publisher;

            if (!publisherDetails.TryGetValue(endpoint, out publisher))
            {
                publisher = new PublisherDetails(endpoint);

                publisherDetails[endpoint] = publisher;
            }

            publisher.RegisterOwnedEvent<T>();
        }

        Dictionary<Type, PublisherDetails> publisherDetails = new Dictionary<Type, PublisherDetails>();

        public class PublisherDetails
        {
            public PublisherDetails(Type publisherTypeType)
            {
                PublisherType = publisherTypeType;
            }

            public List<Type> Events { get; } = new List<Type>();

            public Type PublisherType { get; set; }

            public void RegisterOwnedEvent<T>()
            {
                var eventType = typeof(T);

                if (Events.Contains(eventType))
                {
                    return;
                }

                Events.Add(eventType);
            }
        }
    }
}