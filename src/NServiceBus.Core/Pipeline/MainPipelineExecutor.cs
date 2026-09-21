#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
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
    PipelineMetrics pipelineMetrics,
    EnvelopeUnwrapper envelopeUnwrapper)
    : IPipelineExecutor
{
    public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var pipelineStartedAt = DateTimeOffset.UtcNow;

        using var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

        var childScope = rootBuilder.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            using var incomingMessageHandle = envelopeUnwrapper.UnwrapEnvelope(messageContext);
            IncomingMessage message = incomingMessageHandle;

            //This needs to happen after envelope unwrapping to ensure the proper value of the EnclosedMessageTypes header
            using var activeMessageScope = pipelineMetrics.TrackMessageProcessing(messageContext.IncomingMetricTags, message);

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

                await receivePipeline.Invoke(transportReceiveContext).ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
            catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
            {
                activityFactory.RecordError(activity, ex, transportReceiveContext.Extensions);
                ex.Data["Message ID"] = message.MessageId;

                if (message.NativeMessageId != message.MessageId)
                {
                    ex.Data["Transport message ID"] = message.NativeMessageId;
                }

                ex.Data["Pipeline canceled"] = transportReceiveContext.CancellationToken.IsCancellationRequested;

                if (!ex.IsCausedBy(transportReceiveContext.CancellationToken))
                {
                    pipelineMetrics.RecordMessageProcessingFailure(messageContext.IncomingMetricTags, ex);
                }
                throw;
            }
            finally
            {
                pipelineMetrics.RecordFetchedMessage(messageContext.IncomingMetricTags);
            }

            var completedAt = DateTimeOffset.UtcNow;
            await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, completedAt), cancellationToken).ConfigureAwait(false);
        }
    }
}