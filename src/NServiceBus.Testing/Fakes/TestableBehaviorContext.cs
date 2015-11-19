namespace NServiceBus.Testing.Fakes
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    public class TestableBehaviorContext : BehaviorContext
    {
        public ContextBag Extensions { get; set; } = new ContextBag();
        public IBuilder Builder { get; set; } = new FakeBuilder();
    }
}