namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;

class UnsubscribeDiagnosticsBehavior : IBehavior<IUnsubscribeContext, IUnsubscribeContext>
{
    public Task Invoke(IUnsubscribeContext context, Func<IUnsubscribeContext, Task> next)
    {
        if (Activity.Current != null && context.Extensions.TryGet(DiagnosticsKeys.OutgoingActivityKey, out Activity activity) && activity.IsAllDataRequested)
        {
            //TODO unsubscribe is always a single event type, should the tag name reflect that?
            activity?.SetTag("nservicebus.event_types", context.EventType.FullName);
        }

        return next(context);
    }
}