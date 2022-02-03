namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Sagas;

    class InvokeSagaNotFoundBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            var invocationResult = new SagaInvocationResult();
            context.Extensions.Set(invocationResult);

            await next(context).ConfigureAwait(false);

            if (invocationResult.WasFound)
            {
                return;
            }

            logger.InfoFormat("Could not find any started sagas for message type '{0}'. Going to invoke SagaNotFoundHandlers.", context.Message.MessageType.FullName);

            foreach (var handler in context.Builder.GetServices<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);

                await handler
                    .Handle(context.Message.Instance, context)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }
        }

        static readonly ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();
    }
}