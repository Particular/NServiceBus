namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// A base class for satellite behaviors.
    /// </summary>
    public abstract class SatelliteBehavior: PhysicalMessageProcessingStageBehavior
    {
        /// <summary>
        /// Called when the behavior is executed.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="next">The next <see cref="!:IBehavior{TContext}" /> in the chain to execute.</param>
        public override void Invoke(Context context, Action next)
        {
            Handle(context.GetPhysicalMessage());
        }

        /// <summary>
        /// Handles the dequeued message. Should return true if the message has been processed successfully.
        /// </summary>
        protected abstract bool Handle(TransportMessage physicalMessage);
    }
}