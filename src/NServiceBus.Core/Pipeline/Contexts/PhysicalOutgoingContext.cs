namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class PhysicalOutgoingContextStageBehavior : Behavior<PhysicalOutgoingContextStageBehavior.Context>
    {
        /// <summary>
        /// 
        /// </summary>
        public class Context : OutgoingContext
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="transportMessage"></param>
            /// <param name="parentContext"></param>
            public Context(TransportMessage transportMessage, OutgoingContext parentContext)
                : base(parentContext)
            {
                Set(transportMessage);
            }

            /// <summary>
            /// The message about to be sent out.
            /// </summary>
            public TransportMessage OutgoingMessage
            {
                get { return Get<TransportMessage>(); }
            }

        }
    }
}