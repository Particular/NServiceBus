namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Unicast;

    class LoadHandlersConnector : StageConnector<IUnitOfWorkContext, IInvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
        }

        public override async Task Invoke(IUnitOfWorkContext context, Func<IInvokeHandlerContext, Task> stage)
        {
            var handlersToInvoke = messageHandlerRegistry.GetHandlersFor(context.MessageMetadata.MessageType);

            if (!context.MessageHandled && handlersToInvoke.Count == 0)
            {
                var error = $"No handlers could be found for message type: {context.MessageMetadata.MessageType}";
                throw new InvalidOperationException(error);
            }

            foreach (var messageHandler in handlersToInvoke)
            {
                messageHandler.Instance = context.Builder.Build(messageHandler.HandlerType);

                var handlingContext = this.CreateInvokeHandlerContext(messageHandler, context);
                await stage(handlingContext).ConfigureAwait(false);

                if (handlingContext.HandlerInvocationAborted)
                {
                    //if the chain was aborted skip the other handlers
                    break;
                }
            }
            context.MessageHandled = true;
        }

        MessageHandlerRegistry messageHandlerRegistry;
    }
}