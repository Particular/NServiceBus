namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

    class BehaviorChain<T> where T : BehaviorContext
    {
        public BehaviorChain(IEnumerable<Type> behaviorList)
        {
            foreach (var behaviorType in behaviorList)
            {
                itemDescriptors.Enqueue(behaviorType);
            }
        } 

        public void Invoke(T context)
        {
            InvokeNext(context);
        }

        void InvokeNext(T context)
        {
            if (itemDescriptors.Count == 0 || context.ChainAborted)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            var instance = (IBehavior<T>)context.Builder.Build(behaviorType);

            instance.Invoke(context, () => InvokeNext(context));
        }


        Queue<Type> itemDescriptors = new Queue<Type>();
    }
}