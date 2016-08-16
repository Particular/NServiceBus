namespace NServiceBus.Features
{
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
            var entries = publisherSources.SelectMany(s => s.Generate(conventions)).ToList();
            publishers.AddOrReplacePublishers("EndpointConfiguration", entries);
        }
    }
}