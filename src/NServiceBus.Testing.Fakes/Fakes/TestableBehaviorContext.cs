namespace NServiceBus.Testing
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    /// <summary>
    /// A base implementation for contexts implementing <see cref="IBehaviorContext"/>.
    /// </summary>
    public abstract class TestableBehaviorContext : IBehaviorContext
    {
        /// <summary>
        /// A fake <see cref="IBuilder" /> implementation. If you want to provide your own <see cref="IBuilder" /> implementation
        /// override <see cref="GetBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        /// <summary>
        /// A <see cref="T:NServiceBus.Extensibility.ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; set; } = new ContextBag();

        IBuilder IBehaviorContext.Builder { get; }

        /// <summary>
        /// Selects the builder returned by <see cref="IBehaviorContext.Builder" />. Override this method to provide your custom
        /// <see cref="IBuilder" /> implementation.
        /// </summary>
        protected virtual IBuilder GetBuilder()
        {
            return Builder;
        }
    }
}