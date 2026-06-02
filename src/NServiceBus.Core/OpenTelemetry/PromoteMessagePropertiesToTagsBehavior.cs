#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Pipeline;

class IncomingMessagePayloadToTagsBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
{
    public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
    {
        var activity = Activity.Current;
        if (activity?.IsAllDataRequested == true)
        {
            PromoteProperties(activity, context.Message.Instance);
        }

        return next(context);
    }

   // this needs to be changed to be base64 encoding of the entire body not just the properties

    internal static void PromoteProperties(Activity activity, object instance)
    {
        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(instance);
            if (value is not null)
            {
                activity.SetTag($"nservicebus.message.{property.Name}", value.ToString());
            }
        }
    }
}

class OutgoingMessagePayloadToTagsBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
{
    public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
    {
        var activity = Activity.Current;
        if (activity?.IsAllDataRequested == true)
        {
            IncomingMessagePayloadToTagsBehavior.PromoteProperties(activity, context.Message.Instance);
        }

        return next(context);
    }
}
