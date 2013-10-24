namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using ObjectBuilder;

    class BehaviorChain
    {
        static ILog log = LogManager.GetLogger(typeof(BehaviorChain));

        readonly Queue<BehaviorChainItemDescriptor> itemDescriptors = new Queue<BehaviorChainItemDescriptor>();
        readonly IBuilder builder;

        public BehaviorChain(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Add<TBehavior>(Action<TBehavior> init = null) where TBehavior : IBehavior
        {
            itemDescriptors.Enqueue(new BehaviorChainItemDescriptor(typeof(TBehavior), init ?? (x => { })));
        }

        public void Invoke(TransportMessage incomingTransportMessage)
        {
            using (var context = new BehaviorContext(builder, incomingTransportMessage))
            {
                Invoke(context);
            }

            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Invoked behavior chain: {0}", this);
            }
        }

        internal void Invoke(BehaviorContext context)
        {
            try
            {
                InvokeNext(context);
            }
            catch (Exception exception)
            {
                var error = string.Format("An error occurred while attempting to invoke the following behavior chain: {0}", this);

                throw new Exception(error, exception);
            }
        }

        void InvokeNext(BehaviorContext context)
        {
            if (itemDescriptors.Count != 0 || context.ChainAborted)
            {
                var descriptor = itemDescriptors.Dequeue();
                var instance = descriptor.GetInstance(builder);
                instance.Invoke(context, () => InvokeNext(context));
            }
        }
    }
}