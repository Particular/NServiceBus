namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;

    internal class BehaviorChain
    {
        public BehaviorChain(IBuilder builder, BehaviorContextStacker contextStacker)
        {
            this.builder = builder;
            this.contextStacker = contextStacker;
        }

        public void Add<TBehavior>() where TBehavior : IBehavior
        {
            itemDescriptors.Enqueue(typeof(TBehavior));
        }

        public void Invoke(TransportMessage incomingTransportMessage)
        {
            using (var context = new BehaviorContext(builder, incomingTransportMessage, contextStacker))
            {
                Invoke(context);
            }
        }

        internal void Invoke(BehaviorContext context)
        {
            InvokeNext(context);
        }

        void InvokeNext(BehaviorContext context)
        {
            if (itemDescriptors.Count == 0 || context.ChainAborted)
            {
                return;
            }

            var behaviorType = itemDescriptors.Dequeue();
            var instance = GetInstance(behaviorType);
            instance.Invoke(context, () => InvokeNext(context));
        }


        IBehavior GetInstance(Type type)
        {
            try
            {
                return (IBehavior) builder.Build(type);
            }
            catch (Exception exception)
            {
                var error = string.Format("An error occurred while attempting to create an instance of {0}", type);
                throw new Exception(error, exception);
            }
        }

        readonly BehaviorContextStacker contextStacker;
        IBuilder builder;
        Queue<Type> itemDescriptors = new Queue<Type>();
    }
}