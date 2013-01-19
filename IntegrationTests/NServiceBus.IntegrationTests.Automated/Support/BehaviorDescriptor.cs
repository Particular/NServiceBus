namespace NServiceBus.IntegrationTests.Automated.Support
{
    public class BehaviorDescriptor
    {
        public BehaviorDescriptor(BehaviorContext context, BehaviorFactory factory)
        {
            this.Context = context;
            this.Factory = factory;
        }

        public BehaviorContext Context { get; private set; }

        public BehaviorFactory Factory { get; private set; }
    }
}