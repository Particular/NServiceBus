namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Routing.MessageDrivenSubscriptions;

    class ConfiguredPublishers
    {
        List<IPublisherSource> publisherSources = new List<IPublisherSource>();

        public void Add(IPublisherSource publisherSource)
        {
            Guard.AgainstNull(nameof(publisherSource), publisherSource);
            publisherSources.Add(publisherSource);
        }

        public void Apply(Publishers publishers, Conventions conventions)
        {
            var entries = new Dictionary<Type, PublisherTableEntry>();
            foreach (var source in publisherSources.OrderBy(x => x.Priority)) //Higher priority routes sources override lowe priority.
            {
                foreach (var entry in source.Generate(conventions))
                {
                    entries[entry.EventType] = entry;
                }
            }
            publishers.AddOrReplacePublishers("EndpointConfiguration", entries.Values.ToList());
        }
    }
}