#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;
using Sagas;

class InvokeHandlerTerminator(PipelineMetrics messagingMetricsMeters) : PipelineTerminator<IInvokeHandlerContext>
{
    protected override async Task Terminate(IInvokeHandlerContext context)
    {
        if (context.MessageHandler.Instance is null)
        {
            throw new Exception("MessageHandler.Instance should not be null");
        }

        if (context.Extensions.TryGet<ActiveSagaInstance>(out var saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
        {
            return;
        }

        var messageHandler = context.MessageHandler;

        // Might as well abort before invoking the handler if we're shutting down
        context.CancellationToken.ThrowIfCancellationRequested();
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await messageHandler
                .Invoke(context.MessageBeingHandled, context)
                .ThrowIfNull()
                .ConfigureAwait(false);

            messagingMetricsMeters.RecordSuccessfulMessageHandlerTime(context, Stopwatch.GetElapsedTime(startTimestamp));
        }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
        catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var failureTime = DateTimeOffset.UtcNow;

            ex.Data["Message type"] = context.MessageMetadata.MessageType.FullName;
            ex.Data["Handler type"] = context.MessageHandler.HandlerType.FullName;
            ex.Data["Handler start time"] = DateTimeOffsetHelper.ToWireFormattedString(failureTime - elapsed);
            ex.Data["Handler failure time"] = DateTimeOffsetHelper.ToWireFormattedString(failureTime);
            ex.Data["Handler canceled"] = context.CancellationToken.IsCancellationRequested;

            messagingMetricsMeters.RecordFailedMessageHandlerTime(context, elapsed, ex);
            throw;
        }
    }
}