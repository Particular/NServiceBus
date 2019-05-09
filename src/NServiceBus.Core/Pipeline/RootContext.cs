namespace NServiceBus
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