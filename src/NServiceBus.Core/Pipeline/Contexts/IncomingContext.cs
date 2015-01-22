namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// The first context in the incoming behavior chain
    /// </summary>
    public class IncomingContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="transportMessage">The incoming <see cref="TransportMessage"/>.</param>
        public IncomingContext(BehaviorContext parentContext, TransportMessage transportMessage)
        /// <summary>
        /// The first context in the incoming behavior chain
        /// </summary>
        /// <param name="parentContext"></param>
        public IncomingContext(BehaviorContext parentContext) 
            : base(parentContext)
        {
        }
    }
}