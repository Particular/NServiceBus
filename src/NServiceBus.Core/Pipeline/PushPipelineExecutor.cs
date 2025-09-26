namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Transport;

class PushPipelineExecutor(
    IServiceProvider rootBuilder,
    IPipelineCache pipelineCache,
    MessageOperations messageOperations,
    IPipeline<ITransportReceiveContext> receivePipeline) : IPipelineExecutor
{
    public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        bool createdScope = false;
        if (!messageContext.Extensions.TryGet<AsyncServiceScope>(out var childScope))
        {
            messageContext.Extensions.TryGet<IServiceProvider>(out var serviceProvider);

            childScope = (serviceProvider ?? rootBuilder).CreateAsyncScope();
            createdScope = true;
        }

        try
        {
            var message = new IncomingMessage(messageContext.NativeMessageId, messageContext.Headers,
                messageContext.Body);

            var transportReceiveContext = new TransportReceiveContext(
                childScope.ServiceProvider,
                messageOperations,
                pipelineCache,
                message,
                messageContext.TransportTransaction,
                messageContext.Extensions,
                cancellationToken);

            await receivePipeline.Invoke(transportReceiveContext).ConfigureAwait(false);
        }
        finally
        {
            if (createdScope)
            {
                await childScope.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}