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

            var message = new IncomingMessage(messageContext.MessageId, messageContext.Headers, messageContext.Body);
            using (var childBuilder = builder.CreateChildBuilder())
            {
                

                var rootContext = pipelineComponent.CreateRootContext(childBuilder, messageContext.Extensions);
                var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, messageContext.ReceiveCancellationTokenSource, rootContext);

                await transportReceiveContext.InvokePipeline<ITransportReceiveContext>().ConfigureAwait(false);

                await transportReceiveContext.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }

            
        }

        readonly IBuilder builder;
        readonly PipelineComponent pipelineComponent;
    }
}