namespace NServiceBus.Routing
{
    using System;

    /// <summary>
    /// Safe disconnect event data
    /// </summary>
    public class SafeDisconnect
    {
        readonly TimeSpan idleTimeWaited;

        
        /// <summary>
        /// Creates a new instance of <see cref="SafeDisconnect"/>.
        /// </summary>
        /// <param name="idleTimeWaited">Time waited before raising event.</param>
        public SafeDisconnect(TimeSpan idleTimeWaited)
        {
            this.idleTimeWaited = idleTimeWaited;
        }

        /// <summary>
        /// Time waited before raising event.
        /// </summary>
        public TimeSpan IdleTimeWaited
        {
            get { return idleTimeWaited; }
        }
    }
}