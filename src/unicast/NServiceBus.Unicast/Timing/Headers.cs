namespace NServiceBus.Unicast.Timing
{
    /// <summary>
    /// Timing related headers
    /// </summary>
    public class Headers
    {
        /// <summary>
        /// The time processing of this message started
        /// </summary>
        public const string ProcessingStarted = "NServiceBus.ProcessingStarted";
        
        /// <summary>
        /// The time processing of this message ended
        /// </summary>
        public const string ProcessingEnded = "NServiceBus.ProcessingEnded";

        /// <summary>
        /// The time this message was sent from the client
        /// </summary>
        public const string TimeSent = "NServiceBus.TimeSent";
    }
}