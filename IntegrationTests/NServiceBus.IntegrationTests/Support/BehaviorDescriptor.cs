namespace NServiceBus.IntegrationTests.Support
{
    using System;

    public class BehaviorDescriptor
    {
        private readonly Func<BehaviorContext> behaviorContextBuilder;

        public BehaviorDescriptor(Func<BehaviorContext> behaviorContextBuilder, BehaviorFactory factory)
        {
            this.behaviorContextBuilder = behaviorContextBuilder;
            this.Factory = factory;
        }
        public void Init()
        {
            Context = behaviorContextBuilder();
        }

        public BehaviorContext Context { get; private set; }

        public BehaviorFactory Factory { get; private set; }
    }
}