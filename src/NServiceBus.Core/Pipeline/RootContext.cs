namespace NServiceBus.Pipeline.Contexts
{
    using ObjectBuilder;

    class RootContext : BehaviorContextImpl
    {
        public RootContext(IBuilder builder) : base(null)
        {
            //TODO DanielTim: Should we guard parameters against null and provide a FakeContext for testing?
            Set(builder);
        }
    }
}