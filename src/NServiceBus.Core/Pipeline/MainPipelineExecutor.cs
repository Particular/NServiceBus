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
    IncomingPipelineMetrics incomingPipelineMetrics,
    EnvelopeUnwrapper envelopeUnwrapper)
    : IPipelineExecutor
{
    public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var pipelineStartedAt = DateTimeOffset.UtcNow;
        using var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

        var incomingPipelineMetricsTags = messageContext.Extensions.Get<IncomingPipelineMetricTags>();

        incomingPipelineMetrics.AddDefaultIncomingPipelineMetricTags(incomingPipelineMetricsTags);

        var childScope = rootBuilder.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            var message = envelopeUnwrapper.UnwrapEnvelope(messageContext);
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
                transportReceiveContext.SetIncomingPipelineActivity(activity);
            }

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

                if (!ex.IsCausedBy(transportReceiveContext.CancellationToken))
                {
                    incomingPipelineMetrics.RecordMessageProcessingFailure(incomingPipelineMetricsTags, ex);
                }
                throw;
            }
            finally
            {
                incomingPipelineMetrics.RecordFetchedMessage(incomingPipelineMetricsTags);
            }

            var completedAt = DateTimeOffset.UtcNow;
            await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, completedAt), cancellationToken).ConfigureAwait(false);
        }
    }
}