namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Transport;

class MainPipelineExecutor : IPipelineExecutor
{
    public MainPipelineExecutor(IServiceProvider rootBuilder, IPipelineCache pipelineCache, MessageOperations messageOperations, INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification, IPipeline<ITransportReceiveContext> receivePipeline, IActivityFactory activityFactory)
    {
        this.rootBuilder = rootBuilder;
        this.pipelineCache = pipelineCache;
        this.messageOperations = messageOperations;
        this.receivePipelineNotification = receivePipelineNotification;
        this.receivePipeline = receivePipeline;
        this.activityFactory = activityFactory;
    }

    public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var pipelineStartedAt = DateTimeOffset.UtcNow;

        using var activity = activityFactory.StartIncomingPipelineActivity(messageContext);

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

                throw;
            }

            var pipelineCompletedAt = DateTimeOffset.UtcNow;
            var incomingPipelineMetricTags = transportReceiveContext.Extensions.Get<IncomingPipelineMetricTags>();
            if (incomingPipelineMetricTags.IsMetricTagsCollectionEnabled)
            {
                TagList tags;
                incomingPipelineMetricTags.ApplyTags(ref tags, [
                    MeterTags.QueueName,
                    MeterTags.EndpointDiscriminator,
                    MeterTags.MessageType]);

                Meters.ProcessingTime.Record((pipelineCompletedAt - pipelineStartedAt).TotalSeconds, tags);
                if (message.Headers.TryGetDeliverAt(out var startTime) || message.Headers.TryGetTimeSent(out startTime))
                {
                    Meters.CriticalTime.Record((pipelineCompletedAt - startTime).TotalSeconds, tags);
                }
            }

            await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, pipelineCompletedAt), cancellationToken).ConfigureAwait(false);
        }
    }

    readonly IServiceProvider rootBuilder;
    readonly IPipelineCache pipelineCache;
    readonly MessageOperations messageOperations;
    readonly INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification;
    readonly IPipeline<ITransportReceiveContext> receivePipeline;
    readonly IActivityFactory activityFactory;
}