namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using Customization;

/// <summary>
/// Metadata on events and their publishers.
/// </summary>
public class PublisherMetadata
{
    public IEnumerable<PublisherDetails> Publishers => publisherDetails.Values;

    public void RegisterPublisherFor<T>(string endpointName)
    {
        if (!publisherDetails.TryGetValue(endpointName, out var publisher))
        {
            publisher = new PublisherDetails(endpointName);

            publisherDetails[endpointName] = publisher;
        }

        publisher.RegisterOwnedEvent<T>();
    }

    public void RegisterSelfAsPublisherFor<TEventType>(object self) => RegisterPublisherFor<TEventType>(Conventions.EndpointNamingConvention(self.GetType()));

    public void RegisterPublisherFor<TEventType>(Type endpointType) => RegisterPublisherFor<TEventType>(Conventions.EndpointNamingConvention(endpointType));

    readonly Dictionary<string, PublisherDetails> publisherDetails = [];

    public class PublisherDetails(string publisherName)
    {
        public List<Type> Events { get; } = [];

        public string PublisherName { get; } = publisherName;

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