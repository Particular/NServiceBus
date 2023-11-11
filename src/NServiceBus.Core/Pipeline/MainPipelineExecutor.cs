namespace NServiceBus;

using System;
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

        using var activity = activityFactory.StartIncomingActivity(messageContext);

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

            await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
        }
    }

    readonly IServiceProvider rootBuilder;
    readonly IPipelineCache pipelineCache;
    readonly MessageOperations messageOperations;
    readonly INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification;
    readonly IPipeline<ITransportReceiveContext> receivePipeline;
    readonly IActivityFactory activityFactory;
}