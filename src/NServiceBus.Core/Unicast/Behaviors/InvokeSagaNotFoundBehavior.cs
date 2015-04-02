namespace NServiceBus
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;

    class InvokeSagaNotFoundBehavior : LogicalMessageProcessingStageBehavior
    {
        static ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();

        public override void Invoke(Context context, Action next)
        {
            var invocationResult = new SagaInvocationResult();
            context.Set(invocationResult);

            next();

            if (invocationResult.WasFound)
            {
                return;    
            }

            logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.IncomingLogicalMessage.MessageType.FullName);

            foreach (var handler in context.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking old SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                handler.Handle(context.IncomingLogicalMessage.Instance);
            }

            foreach (var handler in context.Builder.BuildAll<IProcessSagaNotFound>())
            {
                logger.DebugFormat("Invoking new SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                handler.Handle(context.IncomingLogicalMessage.Instance, new SagaNotFoundContext());
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