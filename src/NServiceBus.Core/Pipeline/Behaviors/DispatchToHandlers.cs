namespace NServiceBus.Pipeline.Behaviors
{
    using System;

    class DispatchToHandlers : IBehavior
    {
        public IBehavior Next { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            if (context.Messages == null)
            {
                throw new ArgumentException(string.Format("Messages has not been set on the current behavior context: {0} - DispatchToHandlers must be executed AFTER" +
                                                          "having extracted the messages", context));
            }

            foreach (var message in context.Messages)
            {
                if (context.DoNotContinueDispatchingMessageToHandlers) break;

                Dispatch(message);
            }

            Next.Invoke(context);
        }

        void Dispatch(object message)
        {
            // do it!
        }
    }
}