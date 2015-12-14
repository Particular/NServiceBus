namespace NServiceBus
{
    using ObjectBuilder;

    /// <summary>
    /// The root context.
    /// </summary>
    public class RootContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of a root context.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public RootContext(IBuilder builder) : base(null)
        {
            Set(builder);
        }
    }
}