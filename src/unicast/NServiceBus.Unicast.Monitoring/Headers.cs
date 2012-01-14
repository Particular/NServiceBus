namespace NServiceBus.Unicast.Monitoring
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


        /// <summary>
        /// Id of the message that caused this message to be sent
        /// </summary>
        public const string RelatedTo = "NServiceBus.RelatedTo";

        /// <summary>
        /// Header entry key indicating the types of messages contained.
        /// </summary>
        public const string EnclosedMessageTypes = "NServiceBus.EnclosedMessageTypes";
    }
}