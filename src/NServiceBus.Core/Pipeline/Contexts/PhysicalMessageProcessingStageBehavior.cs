namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// A behavior in physical message processing stage
    /// </summary>
    public abstract class PhysicalMessageProcessingStageBehavior : Behavior<PhysicalMessageProcessingStageBehavior.Context>
    {
        /// <summary>
        /// A context of behavior execution in physical message processing stage.
        /// </summary>
        public class Context : TransportReceiveContext
        {
            /// <summary>
            /// Creates new instance.
            /// </summary>
            protected Context(BehaviorContext parentContext)
                : base(parentContext)
            {

            }

            /// <summary>
            /// Creates new instance.
            /// </summary>
            internal Context(TransportReceiveContext parentContext)
                : base(parentContext)
            {
                
            }

            /// <summary>
            /// If set to true the receive operation will be aborted
            /// </summary>
            public bool AbortReceiveOperation { get; set; }

        }
    }
}