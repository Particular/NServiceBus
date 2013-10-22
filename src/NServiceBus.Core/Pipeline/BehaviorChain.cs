namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using ObjectBuilder;

    class BehaviorChain
    {
        static ILog log = LogManager.GetLogger(typeof(BehaviorChain));

        IBuilder builder;
        Queue<BehaviorChainItemDescriptor> items = new Queue<BehaviorChainItemDescriptor>();

        public BehaviorChain(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Add<TBehavior>(Action<TBehavior> init = null) where TBehavior : IBehavior
        {
            if (init == null)
            {
                items.Enqueue(new BehaviorChainItemDescriptor(typeof(TBehavior), new Action<TBehavior>(x => { })));
            }
            else
            {
                items.Enqueue(new BehaviorChainItemDescriptor(typeof(TBehavior), init));
            }
        }

        public void Invoke(TransportMessage incomingTransportMessage)
        {
            using (var context = new BehaviorContext(incomingTransportMessage))
            {
                Invoke(context);
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
                var error = string.Format("An error occurred while attempting to invoke the following behavior chain: {0}", string.Join(" -> ", items));
                throw new Exception(error, exception);
            }
            finally
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(context.GetTrace());
                }
            }
        }

        void InvokeNext(BehaviorContext context)
        {
            if (items.Count != 0)
            {
                var descriptor = items.Dequeue();
                context.Trace("<{0}>", descriptor.BehaviorType);
                var instance = descriptor.GetInstance(builder);

                var cleanupAction = context.TraceContextFor();
                try
                {
                    instance.Invoke(context, () => InvokeNext(context));
                }
                finally
                {
                    cleanupAction();
                    context.Trace("</{0}>", descriptor.BehaviorType);
                }


            }
        }

    }
}