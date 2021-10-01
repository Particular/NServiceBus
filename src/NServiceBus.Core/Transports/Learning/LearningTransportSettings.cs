namespace NServiceBus
{
    /// <summary>
    /// Learning transport configuration settings.
    /// </summary>
    public class LearningTransportSettings : TransportExtensions<LearningTransport>
    {
        internal LearningTransportSettings(LearningTransport transport, RoutingSettings<LearningTransport> routing)
            : base(transport, routing)

        {
        }

        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        /// <param name="storageDir">The storage path.</param>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.StorageDirectory")]
        public LearningTransportSettings StorageDirectory(string storageDir)
        {
            Transport.StorageDirectory = storageDir;

            return this;
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.RestrictPayloadSize")]
        public LearningTransportSettings NoPayloadSizeRestriction()
        {
            Transport.RestrictPayloadSize = false;

            return this;
        }
    }
}
