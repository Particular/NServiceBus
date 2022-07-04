namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class UnsubscribeDiagnosticsBehavior : IBehavior<IUnsubscribeContext, IUnsubscribeContext>
{
    public Task Invoke(IUnsubscribeContext context, Func<IUnsubscribeContext, Task> next)
    {
        if (context.Extensions.TryGetRecordingOutgoingPipelineActivity(out var activity))
        {
            //TODO unsubscribe is always a single event type, should the tag name reflect that?
            activity?.SetTag("nservicebus.event_types", context.EventType.FullName);
        }

        return next(context);
    }
}