namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Logging;
using Pipeline;

class CaptureExceptionBehavior(ConcurrentDictionary<string, bool> failedMessages)
    : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
{
    public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
    {
        failedMessages.AddOrUpdate(context.Message.MessageId, id => true, (id, value) => true);
        Log.Debug($"Processing message {context.Message.MessageId}");

        await next(context).ConfigureAwait(false);

        failedMessages.AddOrUpdate(context.Message.MessageId, id => false, (id, value) => false);
        Log.Debug($"Finished message {context.Message.MessageId}");
    }

    static readonly ILog Log = LogManager.GetLogger<CaptureExceptionBehavior>();
}