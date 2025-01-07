namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using Pipeline;

public class EnforcePublisherMetadataBehavior(string endpointName, PublisherMetadata publisherMetadata) : IBehavior<IOutgoingPublishContext, IOutgoingPublishContext>
{
    public Task Invoke(IOutgoingPublishContext context, Func<IOutgoingPublishContext, Task> next)
    {
        var publisherDetails = publisherMetadata[endpointName];
        if (!publisherDetails.Events.Contains(context.Message.MessageType))
        {
            throw new Exception("The event being published does not have a corresponding mapping in the PublisherMetadata. Please ensure that the event is being published by the correct publisher endpoint.");
        }
        return next(context);
    }
}