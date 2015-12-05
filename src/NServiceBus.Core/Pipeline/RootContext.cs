namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using ObjectBuilder;

    class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder) : base(null)
        {
            //TODO DanielTim: Should we guard parameters against null and provide a FakeContext for testing?
            Set(builder);
        }
    }
}