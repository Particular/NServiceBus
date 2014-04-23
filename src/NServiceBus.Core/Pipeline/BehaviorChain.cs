namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Utils;

    class BehaviorChain<T> where T : BehaviorContext
    {
        // ReSharper disable once StaticFieldInGenericType
        // The number of T's is small and they will all log to the same point due to the typeof(BehaviorChain<>)
        static ILog Log = LogManager.GetLogger(typeof(BehaviorChain<>));
        Queue<Type> itemDescriptors = new Queue<Type>();
        bool stackTracePreserved;

        public BehaviorChain(IEnumerable<Type> behaviorList)
        {
            foreach (var behaviorType in behaviorList)
            {
                itemDescriptors.Enqueue(behaviorType);
            }
        }

        public void Invoke(T context)
        {
            try
            {
                InvokeNext(context);
            }
            catch (Exception exception)
            {
                // ReSharper disable once PossibleIntendedRethrow
                // need to rethrow explicit exception to preserve the stack trace
                throw exception;
            }
        }

        void InvokeNext(T context)
        {
            if (itemDescriptors.Count == 0 || context.ChainAborted)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            Log.Debug(behaviorType.Name);

            try
            {
                var instance = (IBehavior<T>)context.Builder.Build(behaviorType);
                instance.Invoke(context, () => InvokeNext(context));
            }
            catch (Exception exception)
            {
                if (!stackTracePreserved)
                {
                    exception.PreserveStackTrace();
                }
                stackTracePreserved = true;
                // ReSharper disable once PossibleIntendedRethrow
                // need to rethrow explicit exception to preserve the stack trace
                throw exception;
            }
        }
    }
}