﻿namespace NServiceBus
{
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// A behavior in physical message processing stage.
    /// </summary>
    public abstract class PhysicalMessageProcessingStageBehavior : Behavior<PhysicalMessageProcessingStageBehavior.Context>
    {
        /// <summary>
        /// A context of behavior execution in physical message processing stage.
        /// </summary>
        public class Context : IncomingContext
        {
            /// <summary>
            /// The physical message beeing processed.
            /// </summary>
            public TransportMessage Message { get; private set; }

            /// <summary>
            /// Initializes a new instance of <see cref="Context"/>.
            /// </summary>
            public Context(TransportMessage message, BehaviorContext parentContext)
                : base(parentContext)
            {
                Message = message;
            }
        }
    }
}