namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using Logging;

    class BehaviorChain<T> where T : BehaviorContext
    {
        // ReSharper disable once StaticFieldInGenericType
        // The number of T's is small and they will all log to the same point due to the typeof(BehaviorChain<>)
        static ILog logger = LogManager.GetLogger(typeof(BehaviorChain<>));
        Queue<Type> itemDescriptors = new Queue<Type>();
        Stack<Queue<Type>> snapshots = new Stack<Queue<Type>>();
        ExceptionDispatchInfo preservedRootException;

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
                context.SetChain(this);
                InvokeNext(context);
            }
            catch
            {
                if (preservedRootException != null)
                {
                    preservedRootException.Throw();
                }
                throw;
            }
        }

        void InvokeNext(T context)
        {
            if (itemDescriptors.Count == 0)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            logger.Debug(behaviorType.Name);

            try
            {
                var instance = (IBehavior<T>)context.Builder.Build(behaviorType);
                instance.Invoke(context, () => InvokeNext(context));
            }
            catch (Exception exception)
            {
                if (preservedRootException == null)
                {
                    preservedRootException = ExceptionDispatchInfo.Capture(exception);
                }
                throw;
            }
        }

        public void TakeSnapshot()
        {
            snapshots.Push(new Queue<Type>(itemDescriptors));
        }

        public void DeleteSnapshot()
        {
            itemDescriptors = new Queue<Type>(snapshots.Pop());
        }
    }
}