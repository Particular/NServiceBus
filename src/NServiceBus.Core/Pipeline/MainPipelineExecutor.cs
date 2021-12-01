namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {
        public MainPipelineExecutor(IBuilder rootBuilder, IPipelineCache pipelineCache, MessageOperations messageOperations, INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification, Pipeline<ITransportReceiveContext> receivePipeline, ConsecutiveFailuresCircuitBreaker consecutiveFailuresCircuitBreaker)
        {
            this.rootBuilder = rootBuilder;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.receivePipelineNotification = receivePipelineNotification;
            this.receivePipeline = receivePipeline;
            this.consecutiveFailuresCircuitBreaker = consecutiveFailuresCircuitBreaker;
        }

        public async Task Invoke(MessageContext messageContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = rootBuilder.CreateChildBuilder())
            {
                var message = new IncomingMessage(messageContext.MessageId, messageContext.Headers, messageContext.Body);

                var rootContext = new RootContext(childBuilder, messageOperations, pipelineCache);
                rootContext.Extensions.Merge(messageContext.Extensions);

                var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, messageContext.ReceiveCancellationTokenSource, rootContext);

                try
                {
                    await receivePipeline.Invoke(transportReceiveContext).ConfigureAwait(false);

                    // mark success
                    consecutiveFailuresCircuitBreaker.Success();
                }
                catch (Exception e)
                {
                    e.Data["Message ID"] = message.MessageId;
                    if (message.NativeMessageId != message.MessageId)
                    {
                        e.Data["Transport message ID"] = message.NativeMessageId;
                    }

                    await consecutiveFailuresCircuitBreaker.Failure(e).ConfigureAwait(false);

                    throw;
                }

                await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        readonly IBuilder rootBuilder;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification;
        readonly Pipeline<ITransportReceiveContext> receivePipeline;
        readonly ConsecutiveFailuresCircuitBreaker consecutiveFailuresCircuitBreaker;
    }
}