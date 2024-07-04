namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipeline;
using Sagas;

class InvokeHandlerTerminator(IActivityFactory activityFactory, Func<IServiceProvider, IIncomingPipelineMetrics> incomingPipelineMetricsProvider) : PipelineTerminator<IInvokeHandlerContext>
{
    protected override async Task Terminate(IInvokeHandlerContext context)
    {
        if (context.Extensions.TryGet(out ActiveSagaInstance saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
        {
            return;
        }

        using var activity = activityFactory.StartHandlerActivity(context.MessageHandler, saga);

        var messageHandler = context.MessageHandler;

        // TODO: this is effectively using a provider pattern. It would be better to make that explicit allowing the
        // provider to return a NoOpMessagingMetricsMeters kind of meter
        var messagingMetricsMeters = incomingPipelineMetricsProvider.Invoke(context.Builder);

        // Might as well abort before invoking the handler if we're shutting down
        context.CancellationToken.ThrowIfCancellationRequested();
        var startTime = DateTimeOffset.UtcNow;
        try
        {
            await messageHandler
                .Invoke(context.MessageBeingHandled, context)
                .ThrowIfNull()
                .ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Ok);
            messagingMetricsMeters.RecordSuccessfulMessageHandlerTime(context, DateTimeOffset.UtcNow - startTime);
        }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
        catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
        {
            ex.Data["Message type"] = context.MessageMetadata.MessageType.FullName;
            ex.Data["Handler type"] = context.MessageHandler.HandlerType.FullName;
            ex.Data["Handler start time"] = DateTimeOffsetHelper.ToWireFormattedString(startTime);
            ex.Data["Handler failure time"] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
            ex.Data["Handler canceled"] = context.CancellationToken.IsCancellationRequested;

            activity?.SetErrorStatus(ex);
            messagingMetricsMeters.RecordFailedMessageHandlerTime(context, DateTimeOffset.UtcNow - startTime, ex);
            throw;
        }
    }

}