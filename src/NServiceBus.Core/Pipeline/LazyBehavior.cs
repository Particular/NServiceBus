namespace NServiceBus.Pipeline
{
    using System;
    using ObjectBuilder;

    /// <summary>
    /// Behavior implementation that can wrap another behavior, lizily resolving it from the builder when it is time
    /// </summary>
    public class LazyBehavior<TBehavior> : IBehavior
        where TBehavior : IBehavior
    {
        readonly IBuilder builder;
        readonly Delegate initializationMethod;

        public LazyBehavior(IBuilder builder, Delegate initializationMethod)
        {
            this.builder = builder;
            this.initializationMethod = initializationMethod;
        }

        public IBehavior Next { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var behaviorInstance = BuildBehaviorInstance();
            InitializeInstance(behaviorInstance);
            behaviorInstance.Next = Next;
            behaviorInstance.Invoke(context);
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
                throw new ApplicationException(
                    string.Format("An error occured when initializing behavior {0}", behaviorInstance), exception);
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
                throw new ApplicationException(
                    string.Format("Could not build behavior instance of type {0}", typeof(TBehavior)), exception);
            }
        }
    }
}