namespace NServiceBus.Pipeline
{
    using System;
    using ObjectBuilder;

    /// <summary>
    /// Behavior implementation that can wrap another behavior, lazily resolving it from the builder when it is time
    /// </summary>
    class LazyBehavior<TBehavior> : IBehavior
        where TBehavior : IBehavior
    {
        IBuilder builder;
        Delegate initializationMethod;

        public LazyBehavior(IBuilder builder, Delegate initializationMethod)
        {
            this.builder = builder;
            this.initializationMethod = initializationMethod;
        }

        public IBehavior Next { get; set; }

        public void Invoke(BehaviorContext context)
        {
            var behaviorInstance = BuildBehaviorInstance();
            InitializeInstance(behaviorInstance);
            behaviorInstance.Next = Next;

            context.Trace("<{0}>", typeof(TBehavior));
            var cleanupAction = context.TraceContextFor<TBehavior>();
            try
            {
                behaviorInstance.Invoke(context);
            }
            finally
            {
                cleanupAction();
                context.Trace("</{0}>", typeof(TBehavior));
            }
        }

        TBehavior BuildBehaviorInstance()
        {
            try
            {
                return builder.Build<TBehavior>();
            }
            catch (Exception exception)
            {
                var error = string.Format("Could not build behavior instance of type {0}", typeof(TBehavior));
                throw new Exception(error, exception);
            }
        }

        void InitializeInstance(TBehavior behaviorInstance)
        {
            if (initializationMethod == null) return;
            try
            {
                initializationMethod.DynamicInvoke(behaviorInstance);
            }
            catch (Exception exception)
            {
                var error = string.Format("An error occurred when initializing behavior {0}", behaviorInstance);
                throw new Exception(error, exception);
            }
        }
    }
}