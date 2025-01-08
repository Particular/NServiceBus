namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pipeline;

public class EnforceSubscriptionPublisherMetadataBehavior(string endpointName, PublisherMetadata publisherMetadata) : IBehavior<ISubscribeContext, ISubscribeContext>
{
    readonly Dictionary<Type, bool> eventTypeMap = publisherMetadata.Publishers.SelectMany(publisher => publisher.Events).ToDictionary(k => k, v => true);

    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        var unmappedEventTypes = new List<Type>(context.EventTypes.Where(eventType => !eventTypeMap.ContainsKey(eventType)));
        if (unmappedEventTypes.Count != 0)
        {
            throw new Exception($"The event(s) '{string.Join(", ", unmappedEventTypes)}' being subscribed to by '{endpointName}' do(es) not have a corresponding mapping in the PublisherMetadata.");
        }
        return next(context);
    }
}