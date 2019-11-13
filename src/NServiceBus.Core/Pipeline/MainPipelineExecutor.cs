namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {
        public MainPipelineExecutor(IBuilder builder, PipelineComponent pipelineComponent, SendComponent sendComponent)
        {
            this.builder = builder;
            this.pipelineComponent = pipelineComponent;
            this.sendComponent = sendComponent;
        }

        public async Task Invoke(MessageContext messageContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = builder.CreateChildBuilder())
            {
                var message = new IncomingMessage(messageContext.MessageId, messageContext.Headers, messageContext.Body);

                var rootContext = pipelineComponent.CreateRootContext(childBuilder, sendComponent.CreateMessageOperations(builder, pipelineComponent), messageContext.Extensions);
                var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, messageContext.ReceiveCancellationTokenSource, rootContext);

                try
                {
                    await transportReceiveContext.InvokePipeline<ITransportReceiveContext>().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.Data["Message ID"] = message.MessageId;
                    if (message.NativeMessageId != message.MessageId)
                    {
                        e.Data["Transport message ID"] = message.NativeMessageId;
                    }

                    throw;
                }

                await transportReceiveContext.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        readonly IBuilder builder;
        readonly PipelineComponent pipelineComponent;
        readonly SendComponent sendComponent;
    }
}