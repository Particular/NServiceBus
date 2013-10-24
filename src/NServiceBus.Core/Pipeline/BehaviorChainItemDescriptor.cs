namespace NServiceBus.Pipeline
{
    using System;
    using ObjectBuilder;

    /// <summary>
    /// Chain item descriptor that will help create a behavior instance
    /// </summary>
    class BehaviorChainItemDescriptor
    {
        readonly Delegate initializationMethod;
        public Type BehaviorType;

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