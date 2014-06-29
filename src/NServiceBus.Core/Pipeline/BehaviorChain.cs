namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using NServiceBus.Pipeline;

    class BehaviorChain<T> where T : BehaviorContext
    {
        T context;
        // ReSharper disable once StaticFieldInGenericType
        // The number of T's is small and they will all log to the same point due to the typeof(BehaviorChain<>)
        static ILog logger = LogManager.GetLogger(typeof(BehaviorChain<>));
        Queue<Type> itemDescriptors = new Queue<Type>();
        Stack<Queue<Type>> snapshots = new Stack<Queue<Type>>();
        
        public BehaviorChain(IEnumerable<Type> behaviorList, T context)
        {
            context.SetChain(this);
            this.context = context;
            foreach (var behaviorType in behaviorList)
            {
                itemDescriptors.Enqueue(behaviorType);
            }
        }

        public void Invoke()
        {
            if (itemDescriptors.Count == 0)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            logger.Debug(behaviorType.Name);

            var instance = (IBehavior<T>) context.Builder.Build(behaviorType);
            
            instance.Invoke(context, Invoke);
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
