namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Pipeline;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeSagaNotFoundBehavior : Behavior<LogicalMessageProcessingContext>
    {
        static ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();

        public override async Task Invoke(LogicalMessageProcessingContext context, Func<Task> next)
        {
            var invocationResult = new SagaInvocationResult();
            context.Set(invocationResult);

            await next().ConfigureAwait(false);

            if (invocationResult.WasFound)
            {
                return;    
            }

            logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.Message.MessageType.FullName);

            foreach (var handler in context.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                await handler.Handle(context.Message.Instance, new MessageProcessingContext(context));
            }
        }
    }
}