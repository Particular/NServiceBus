namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Routing.MessageDrivenSubscriptions;

class ConfiguredPublishers
{
    readonly List<IPublisherSource> publisherSources = [];

    public void Add(IPublisherSource publisherSource)
    {
        ArgumentNullException.ThrowIfNull(publisherSource);
        publisherSources.Add(publisherSource);
    }

    public void Apply(Publishers publishers, Conventions conventions, bool enforceBestPractices)
    {
        var entries = publisherSources.SelectMany(s => Generate(conventions, s, enforceBestPractices)).ToList();
        publishers.AddOrReplacePublishers("EndpointConfiguration", entries);
    }

    static IEnumerable<PublisherTableEntry> Generate(Conventions conventions, IPublisherSource source, bool enforceBestPractices)
    {
        return enforceBestPractices
            ? source.GenerateWithBestPracticeEnforcement(conventions)
            : source.GenerateWithoutBestPracticeEnforcement(conventions);
    }
}