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
        Type behaviorType;
        IBuilder builder;
        Delegate initializationMethod;

        public BehaviorChainItemDescriptor(Type behaviorType, IBuilder builder, Delegate initializationMethod = null)
        {
            this.behaviorType = behaviorType;
            this.builder = builder;
            this.initializationMethod = initializationMethod;
        }

        public IBehavior GetInstance()
        {
            try
            {
                var wrapperType = typeof(LazyBehavior<>).MakeGenericType(behaviorType);
                var instance = Activator.CreateInstance(wrapperType, new object[] { builder, initializationMethod });
                return (IBehavior)instance;
            }
            catch (Exception exception)
            {
                var error = string.Format("An error occurred while attempting to create an instance of {0} closed with {1}", typeof(LazyBehavior<>), behaviorType);
                throw new Exception(error, exception);
            }
        }

        public override string ToString()
        {
            return behaviorType.Name;
        }
    }
}