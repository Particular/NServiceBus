namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.ObjectPool;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {

        public MainPipelineExecutor(IServiceProvider rootBuilder, IPipelineCache pipelineCache, MessageOperations messageOperations, INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification, Pipeline<ITransportReceiveContext> receivePipeline)
        {
            this.rootBuilder = rootBuilder;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.receivePipelineNotification = receivePipelineNotification;
            this.receivePipeline = receivePipeline;
        }

        public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
        {
            var pipelineStartedAt = DateTimeOffset.UtcNow;

            using (var childScope = rootBuilder.CreateScope())
            {
                var headers = HeaderPool.Get();
                messageContext.Headers.CopyTo(headers);

                var message = new IncomingMessage(messageContext.NativeMessageId, headers, messageContext.Body);

                var rootContext = new RootContext(childScope.ServiceProvider, messageOperations, pipelineCache, cancellationToken);
                rootContext.Extensions.Merge(messageContext.Extensions);

                var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, rootContext);
                try
                {
                    try
                    {
                        await receivePipeline.Invoke(transportReceiveContext).ConfigureAwait(false);
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

                        ex.Data["Pipeline canceled"] =
                            transportReceiveContext.CancellationToken.IsCancellationRequested;

                        throw;
                    }

                    await receivePipelineNotification
                        .Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTimeOffset.UtcNow),
                            cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    HeaderPool.Return(headers);
                }
            }
        }

        readonly IServiceProvider rootBuilder;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification;
        readonly Pipeline<ITransportReceiveContext> receivePipeline;

        internal static readonly ObjectPool<Dictionary<string, string>> HeaderPool =
            new DefaultObjectPool<Dictionary<string, string>>(
                new DictionaryPooledObjectPolicy(), 100);
    }

    static class DictionaryExtensions
    {
        public static void CopyTo(this Dictionary<string, string> dictionary, Dictionary<string, string> target)
        {
            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                target.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    class DictionaryPooledObjectPolicy : PooledObjectPolicy<Dictionary<string, string>>
    {
        public override Dictionary<string, string> Create() => new Dictionary<string, string>();

        public override bool Return(Dictionary<string, string> obj)
        {
            obj.Clear();
            return true;
        }
    }
}