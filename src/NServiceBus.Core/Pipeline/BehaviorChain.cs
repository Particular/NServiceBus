namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;

    internal class BehaviorChain<T> where T:BehaviorContext
    {
        public BehaviorChain(IBuilder builder)
        {
            this.builder = builder;
        }

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
            var instance = GetInstance(behaviorType) as IBehavior<T>;
            
            instance.Invoke(context, () => InvokeNext(context));
        }


        object GetInstance(Type type) 
        {
            try
            {
                return builder.Build(type);
            }
            catch (Exception exception)
            {
                var error = string.Format("An error occurred while attempting to create an instance of {0}", type);
                throw new Exception(error, exception);
            }
        }

        IBuilder builder;
        Queue<Type> itemDescriptors = new Queue<Type>();
    }
}