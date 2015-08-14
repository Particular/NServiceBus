namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;

    class InvokeSagaNotFoundBehavior : LogicalMessageProcessingStageBehavior
    {
        static ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();

        public override async Task Invoke(Context context, Func<Task> next)
        {
            var invocationResult = new SagaInvocationResult();
            context.Set(invocationResult);

            await next().ConfigureAwait(false);

            if (invocationResult.WasFound)
            {
                return;    
            }

            logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.MessageType.FullName);

            foreach (var handler in context.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                handler.Handle(context.GetLogicalMessage().Instance);
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("InvokeSagaNotFound", typeof(InvokeSagaNotFoundBehavior), "Invokes saga not found logic")
            {
                InsertAfter(WellKnownStep.MutateIncomingMessages);
            }
        }
    }
}