#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Pipeline;

class IncomingMessagePayloadToTagsBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
{
    public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
    {
        var activity = Activity.Current;
        if (activity?.IsAllDataRequested == true)
        {
            SetMessageBodyTag(activity, context.Message.Instance);
        }

        return next(context);
    }

    internal static void SetMessageBodyTag(Activity activity, object instance)
    {
        var json = JsonSerializer.Serialize(instance, instance.GetType());
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        activity.SetTag("nservicebus.message.body", base64);
    }
}

class OutgoingMessagePayloadToTagsBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
{
    public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
    {
        var activity = Activity.Current;
        if (activity?.IsAllDataRequested)
        {
            IncomingMessagePayloadToTagsBehavior.SetMessageBodyTag(activity, context.Message.Instance);
        }

        return next(context);
    }
}
