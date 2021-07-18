namespace NServiceBus
{
    using System;
    using System.Linq;
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

            var sagaTypes = invocationResult.Results.Where(x => x.Value == SagaInvocationResult.State.SagaNotFound).Select(y => y.Key).ToArray();

            if (sagaTypes.Length == 0)
            {
                return;
            }

            logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.Message.MessageType.FullName);

            foreach (var handler in context.Builder.GetServices<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);

                foreach (var sagaType in sagaTypes)
                {
                    await handler
                        .Handle(context.Message.Instance, context, sagaType)
                        .ThrowIfNull()
                        .ConfigureAwait(false);
                }
            }
        }

        static readonly ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();
    }
}