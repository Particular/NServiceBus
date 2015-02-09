namespace NServiceBus
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Saga;

    class InvokeSagaNotFoundBehavior : IBehavior<IncomingContext>
    {
        static ILog logger = LogManager.GetLogger<InvokeSagaNotFoundBehavior>();

        public void Invoke(IncomingContext context, Action next)
        {
            context.Set("Sagas.InvokeSagaNotFound", false);

            next();

            if (!context.Get<bool>("Sagas.InvokeSagaNotFound"))
            {
               return; 
            }

            bool result;
            if (context.TryGet("Sagas.SagaWasInvoked", out result))
            {
                if (result)
                {
                    return;
                }
            }

            logger.InfoFormat("Could not find a started saga for '{0}' message type. Going to invoke SagaNotFoundHandlers.", context.IncomingLogicalMessage.MessageType.FullName);

            foreach (var handler in context.Builder.BuildAll<IHandleSagaNotFound>())
            {
                logger.DebugFormat("Invoking SagaNotFoundHandler ('{0}')", handler.GetType().FullName);
                handler.Handle(context.IncomingLogicalMessage.Instance);
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("InvokeSagaNotFound", typeof(InvokeSagaNotFoundBehavior), "Invokes saga not found logic")
            {
                InsertBefore(WellKnownStep.LoadHandlers);
                InsertAfter(WellKnownStep.MutateIncomingMessages);
            }
        }
    }
}