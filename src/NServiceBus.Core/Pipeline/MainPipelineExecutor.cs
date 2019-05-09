namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {
        public MainPipelineExecutor(IBuilder builder, PipelineComponent pipelineComponent)
        {
            this.builder = builder;
            this.pipelineComponent = pipelineComponent;
        }

        public async Task Invoke(MessageContext messageContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = builder.CreateChildBuilder())
            {
                var message = new IncomingMessage(messageContext.MessageId, messageContext.Headers, messageContext.Body);

                var context = await pipelineComponent.Invoke<ITransportReceiveContext>(childBuilder, rootContext =>
                {
                    var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, messageContext.ReceiveCancellationTokenSource, rootContext);

                    transportReceiveContext.Extensions.Merge(messageContext.Extensions);

                    return transportReceiveContext;
                }).ConfigureAwait(false);

                await context.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        readonly IBuilder builder;
        readonly PipelineComponent pipelineComponent;
    }
}