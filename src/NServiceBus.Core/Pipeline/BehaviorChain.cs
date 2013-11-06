namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

    internal class BehaviorChain<T> where T : BehaviorContext
    {
        public void Add<TBehavior>() where TBehavior : IBehavior<T>
        {
            itemDescriptors.Enqueue(typeof(TBehavior));
        }

        internal void Invoke(T context)
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
            var instance =  context.Builder.Build(behaviorType) as IBehavior<T>;

            instance.Invoke(context, () => InvokeNext(context));
        }


        Queue<Type> itemDescriptors = new Queue<Type>();
    }
}