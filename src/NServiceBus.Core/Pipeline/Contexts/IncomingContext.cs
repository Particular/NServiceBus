namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// The first context in the incoming behavior chain
    /// </summary>
    public class IncomingContext : BehaviorContext
    {
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