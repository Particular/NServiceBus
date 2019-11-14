namespace NServiceBus
{
    using ObjectBuilder;

    class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder, MessageOperations messageOperations) : base(null)
        {
            Set(messageOperations);
            Set(builder);
        }
    }
}