namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// The first context in the incoming behavior chain.
    /// </summary>
    public abstract class IncomingContext : BehaviorContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IncomingContext"/>.
        /// </summary>
        protected IncomingContext(BehaviorContext parentContext) 
            : base(parentContext)
        {
        }
    }
}