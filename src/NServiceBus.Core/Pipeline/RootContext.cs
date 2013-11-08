namespace NServiceBus.Pipeline
{
    using ObjectBuilder;

    class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder) : base(null)
        {
            Set(builder);
        }
    }
}