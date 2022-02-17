namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Pipeline;

    class AttachSenderRelatedInfoOnMessageBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            var message = context.Message;
            var utcNow = DateTime.UtcNow;

            // This behavior executes in the case of auditing as well, so assuming there are no delayed delivery constraints set,
            // no existing header should be overwritten, otherwise the message being audited would be modified improperly.

            if (!message.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                message.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;
            }

            if (!message.Headers.ContainsKey(Headers.TimeSent))
            {
                message.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(utcNow);
            }

            if (!message.Headers.ContainsKey(Headers.DeliverAt))
            {
                if (context.Extensions.TryGet<SendOptions>(out var options))
                {
                    if (options.DelayedDeliveryConstraint is DelayDeliveryWith delayDeliveryWith)
                    {
                        var timeDelay = delayDeliveryWith.Delay;
                        message.Headers[Headers.DeliverAt] = DateTimeExtensions.ToWireFormattedString(utcNow.Add(timeDelay));
                    }
                    else if (options.DelayedDeliveryConstraint is DoNotDeliverBefore doNotDeliverBefore)
                    {
                        var deliverAt = doNotDeliverBefore.At;
                        message.Headers[Headers.DeliverAt] = DateTimeExtensions.ToWireFormattedString(deliverAt);
                    }
                }
            }
            return next(context);
        }
    }
}