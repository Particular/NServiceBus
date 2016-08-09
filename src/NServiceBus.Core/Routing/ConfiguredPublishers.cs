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

        public void Apply(Publishers publishers)
        {
            var entries = new Dictionary<Type, PublisherTableEntry>();
            foreach (var source in publisherSources.OrderBy(x => x.Priority)) //Higher priority routes sources override lowe priority.
            {
                source.Generate(e => entries[e.EventType] = e);
            }
            publishers.AddOrReplacePublishers("EndpointConfiguration", entries.Values.ToList());
        }
    }
}