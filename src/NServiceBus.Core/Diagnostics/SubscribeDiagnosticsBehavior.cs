namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class SubscribeDiagnosticsBehavior : IBehavior<ISubscribeContext, ISubscribeContext>
{
    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        if (context.Extensions.TryGetRecordingOutgoingPipelineActivity(out var activity))
        {
            //TODO should the tag name change between 1 or multiple event subscriptions? Currently only autosubscribe can register multiple events at once
            activity?.SetTag("nservicebus.event_types", string.Join(",", (object[])context.EventTypes));
        }

        return next(context);
    }
}