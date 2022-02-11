namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Transport;
    using Pipeline;

    class AttachSenderRelatedInfoOnMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            var message = context.Message;
            var utcNow = DateTimeOffset.UtcNow;

            // This behavior executes in the case of auditing as well, so assuming there are no dispatch properties set,
            // no existing header should be overwritten, otherwise the message being audited would be modified improperly.

            if (!message.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                message.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;
            }

            if (!message.Headers.ContainsKey(Headers.TimeSent))
            {
                message.Headers[Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(utcNow);
            }

            if (!message.Headers.ContainsKey(Headers.DeliverAt))
            {
                if (context.Extensions.TryGet<DispatchProperties>(out var dispatchProperties))
                {
                    if (dispatchProperties.DelayDeliveryWith != null)
                    {
                        var timeDelay = dispatchProperties.DelayDeliveryWith.Delay;
                        message.Headers[Headers.DeliverAt] = DateTimeOffsetHelper.ToWireFormattedString(utcNow.Add(timeDelay));
                    }
                    else if (dispatchProperties.DoNotDeliverBefore != null)
                    {
                        message.Headers[Headers.DeliverAt] = DateTimeOffsetHelper.ToWireFormattedString(dispatchProperties.DoNotDeliverBefore.At);
                    }
                }
            }
            return next(context);
        }
    }
}