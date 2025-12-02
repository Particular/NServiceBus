#nullable enable

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
            throw new Exception($"The event '{context.Message.MessageType}' being published by '{endpointName}' does not have a corresponding mapping in the PublisherMetadata. Add the following code to the endpoint configuration builder: metadata.RegisterSelfAsPublisherFor<{context.Message.MessageType}>(this);");
        }
        return next(context);
    }
}