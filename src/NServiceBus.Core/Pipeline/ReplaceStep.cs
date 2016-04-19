namespace NServiceBus
{
    using System;
    using ObjectBuilder;
    using Pipeline;

    class ReplaceStep
    {
        public ReplaceStep(string idToReplace, Type behavior, string description = null, Func<IBuilder, IBehavior> factoryMethod = null)
        {
            ReplaceId = idToReplace;
            Description = description;
            BehaviorType = behavior;
            FactoryMethod = factoryMethod;
        }

        public string ReplaceId { get; }
        public string Description { get; }
        public Type BehaviorType { get; }
        public Func<IBuilder, IBehavior> FactoryMethod { get; }
    }
}