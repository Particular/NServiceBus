namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;

    class CaptureExceptionBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
    {
        public CaptureExceptionBehavior(ConcurrentDictionary<string, bool> failedMessages)
        {
            this.failedMessages = failedMessages;
        }

        public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, CancellationToken, Task> next, CancellationToken token)
        {
            failedMessages.AddOrUpdate(context.Message.MessageId, id => true, (id, value) => true);
            log.Debug($"Processing message {context.Message.MessageId}");

            await next(context, token).ConfigureAwait(false);

            failedMessages.AddOrUpdate(context.Message.MessageId, id => false, (id, value) => false);
            log.Debug($"Finished message {context.Message.MessageId}");
        }

        ConcurrentDictionary<string, bool> failedMessages;

        static ILog log = LogManager.GetLogger<CaptureExceptionBehavior>();
    }
}