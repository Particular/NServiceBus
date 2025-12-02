#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipeline;

public class EnforceSubscriptionPublisherMetadataBehavior(string endpointName, PublisherMetadata publisherMetadata) : IBehavior<ISubscribeContext, ISubscribeContext>
{
    readonly HashSet<Type> eventTypeMap = publisherMetadata.Publishers.SelectMany(publisher => publisher.Events).ToHashSet();

    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        var unmappedEventTypes = new List<Type>(context.EventTypes.Where(eventType => !eventTypeMap.Contains(eventType)));
        if (unmappedEventTypes.Count == 0)
        {
            return next(context);
        }

        var builder = new StringBuilder();
        _ = builder.AppendLine($"The following event(s) are being subscribed to by '{endpointName}' but do not have a corresponding mapping in the PublisherMetadata:");
        foreach (var eventType in unmappedEventTypes)
        {
            _ = builder.AppendLine($"- metadata.RegisterPublisherFor<{eventType}>(\"typeof(PublisherEndpointToBeDetermined)\");");
        }
        throw new Exception(builder.ToString());
    }
}