namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Transport;

class MainPipelineExecutor(
    IServiceProvider rootBuilder,
    IPipelineCache pipelineCache,
    MessageOperations messageOperations,
    INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification,
    IPipeline<ITransportReceiveContext> receivePipeline,
    IActivityFactory activityFactory,
    IncomingPipelineMetrics incomingPipelineMetrics)
    : IPipelineExecutor
{
    public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var pipelineStartedAt = DateTimeOffset.UtcNow;

        using var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

        var incomingPipelineMetricsTags = incomingPipelineMetrics.CreateDefaultIncomingPipelineMetricTags();
        incomingPipelineMetrics.RecordFetchedMessage(incomingPipelineMetricsTags);

        var childScope = rootBuilder.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            var message = new IncomingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

            var transportReceiveContext = new TransportReceiveContext(
                childScope.ServiceProvider,
                messageOperations,
                pipelineCache,
                message,
                messageContext.TransportTransaction,
                messageContext.Extensions,
                cancellationToken);

            if (activity != null)
            {
                transportReceiveContext.SetIncomingPipelineActitvity(activity);
            }

            transportReceiveContext.Extensions.Set(incomingPipelineMetricsTags);

            try
            {
                await receivePipeline.Invoke(transportReceiveContext, activity).ConfigureAwait(false);
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
            catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
            {
                ex.Data["Message ID"] = message.MessageId;

                if (message.NativeMessageId != message.MessageId)
                {
                    ex.Data["Transport message ID"] = message.NativeMessageId;
                }

                ex.Data["Pipeline canceled"] = transportReceiveContext.CancellationToken.IsCancellationRequested;

                incomingPipelineMetrics.RecordMessageProcessingFailure(incomingPipelineMetricsTags, ex);

                throw;
            }

            var completedAt = DateTimeOffset.UtcNow;
            // TODO the following metrics should be recorded only if the Outbox did not dedup the incoming message
            // We should not publish a successfully processed or critical time for a duplicate message
            incomingPipelineMetrics.RecordMessageSuccessfullyProcessed(incomingPipelineMetricsTags);
            if (message.Headers.TryGetDeliverAt(out var startTime) || message.Headers.TryGetTimeSent(out startTime))
            {
                incomingPipelineMetrics.RecordMessageCriticalTime(completedAt - startTime, incomingPipelineMetricsTags);
            }

            await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, completedAt), cancellationToken).ConfigureAwait(false);
        }
    }
}