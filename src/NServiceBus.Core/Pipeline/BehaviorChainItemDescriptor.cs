namespace NServiceBus.Pipeline
{
    using System;
    using ObjectBuilder;

    /// <summary>
    /// Chain item descriptor that will help create a behavior instance. The actual creation of the instance
    /// will be deferred by wrapping it in a <see cref="LazyBehavior{TBehavior}"/> which will use the builder
    /// to build the behavior when it is invoked.
    /// </summary>
    class BehaviorChainItemDescriptor
    {
        public Type BehaviorType;
        Delegate initializationMethod;

        public BehaviorChainItemDescriptor(Type behaviorType, Delegate initializationMethod)
        {
            BehaviorType = behaviorType;
            this.initializationMethod = initializationMethod;
        }

        public IBehavior GetInstance(IBuilder builder)
        {
            try
            {
                var behavior = (IBehavior)builder.Build(BehaviorType);
                 initializationMethod.DynamicInvoke(behavior);
                return behavior;
            }
            catch (Exception exception)
            {
                var error = string.Format("An error occurred while attempting to create an instance of {0}", BehaviorType);
                throw new Exception(error, exception);
            }
        }

    }
}