namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

class SubscribeDiagnosticsBehavior : IBehavior<ISubscribeContext, ISubscribeContext>
{
    public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
    {
        if (Activity.Current != null && context.Extensions.TryGet(DiagnosticsKeys.OutgoingActivityKey, out Activity activity) && activity.IsAllDataRequested)
        {
            //TODO should the tag name change between 1 or multiple event subscriptions? Currently only autosubscribe can register multiple events at once
            activity?.SetTag("nservicebus.event_types", string.Join(",", (object[])context.EventTypes));
        }

        return next(context);
    }
}