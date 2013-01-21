namespace NServiceBus.IntegrationTests.Support
{
    using System;

    public class BehaviorDescriptor
    {
        private readonly Func<BehaviorContext> behaviorContextBuilder;

        public BehaviorDescriptor(Func<BehaviorContext> behaviorContextBuilder, EndpointBuilder factory)
        {
            this.behaviorContextBuilder = behaviorContextBuilder;
            this.Factory = factory;
        }
        public void Init()
        {
            Context = behaviorContextBuilder();
        }

        public BehaviorContext Context { get; private set; }

        public IEndpointBehaviorFactory Factory { get; private set; }
    }
}