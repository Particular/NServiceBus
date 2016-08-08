namespace NServiceBus
{
    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static partial class Headers
    {
        /// <summary>
        /// Header for retrieving from which Http endpoint the message arrived.
        /// </summary>
        public const string HttpFrom = "NServiceBus.From";

        /// <summary>
        /// Header for specifying to which Http endpoint the message should be delivered.
        /// </summary>
        public const string HttpTo = "NServiceBus.To";

        /// <summary>
        /// Header for specifying to which queue behind the http gateway should the message be delivered.
        /// This header is considered an applicative header.
        /// </summary>
        public const string RouteTo = "NServiceBus.Header.RouteTo";

        /// <summary>
        /// Header for specifying to which sites the gateway should send the message. For multiple
        /// sites a comma separated list can be used
        /// This header is considered an applicative header.
        /// </summary>
        public const string DestinationSites = "NServiceBus.DestinationSites";

        /// <summary>
        /// Header for specifying the key for the site where this message originated.
        /// This header is considered an applicative header.
        /// </summary>
        public const string OriginatingSite = "NServiceBus.OriginatingSite";

        /// <summary>
        /// Header containing the id of the saga instance the sent the message
        /// This header is considered an applicative header.
        /// </summary>
        public const string SagaId = "NServiceBus.SagaId";

        /// <summary>
        /// Header containing a stable message id for a message.
        /// </summary>
        public const string MessageId = "NServiceBus.MessageId";

        /// <summary>
        /// Header containing a correlation id for a message.
        /// </summary>
        public const string CorrelationId = "NServiceBus.CorrelationId";

        /// <summary>
        /// Header containing the ReplyToAddress for a message.
        /// </summary>
        public const string ReplyToAddress = "NServiceBus.ReplyToAddress";

        /// <summary>
        /// Prefix included on the wire when sending applicative headers.
        /// </summary>
        public const string HeaderName = "Header";

        /// <summary>
        /// Header telling the NServiceBus Version (beginning NServiceBus V3.0.1).
        /// </summary>
        public const string NServiceBusVersion = "NServiceBus.Version";

        /// <summary>
        /// Used in a header when doing a callback (session.return).
        /// </summary>
        public const string ReturnMessageErrorCodeHeader = "NServiceBus.ReturnMessage.ErrorCode";

        /// <summary>
        /// Header that tells if this transport message is a control message.
        /// </summary>
        public const string ControlMessageHeader = "NServiceBus.ControlMessage";

        /// <summary>
        /// Type of the saga that this message is targeted for.
        /// </summary>
        public const string SagaType = "NServiceBus.SagaType";

        /// <summary>
        /// Id of the saga that sent this message.
        /// </summary>
        public const string OriginatingSagaId = "NServiceBus.OriginatingSagaId";

        /// <summary>
        /// Type of the saga that sent this message.
        /// </summary>
        public const string OriginatingSagaType = "NServiceBus.OriginatingSagaType";

        /// <summary>
        /// The number of Delayed Retries that have been performed for this message.
        /// </summary>
        public const string DelayedRetries = "NServiceBus.Retries";

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(DelayedRetries)
            )]
#pragma warning disable 1591
        public const string Retries = DelayedRetries;
#pragma warning restore 1591

        /// <summary>
        /// The time the last Delayed Retry has been performed for this message.
        /// </summary>
        public const string DelayedRetriesTimestamp = "NServiceBus.Retries.Timestamp";


        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(DelayedRetriesTimestamp)
            )]
#pragma warning disable 1591
        public const string RetriesTimestamp = DelayedRetriesTimestamp;
#pragma warning restore 1591

        /// <summary>
        /// The number of Immediate Retries that have been performed for this message.
        /// </summary>
        public const string ImmediateRetries = "NServiceBus.FLRetries";

        [ObsoleteEx(
            TreatAsErrorFromVersion = "6.0",
            RemoveInVersion = "7.0",
            ReplacementTypeOrMember = nameof(ImmediateRetries)
            )]
#pragma warning disable 1591
        public const string FLRetries = ImmediateRetries;
#pragma warning restore 1591

        /// <summary>
        /// The time processing of this message started.
        /// </summary>
        public const string ProcessingStarted = "NServiceBus.ProcessingStarted";

        /// <summary>
        /// The time processing of this message ended.
        /// </summary>
        public const string ProcessingEnded = "NServiceBus.ProcessingEnded";

        /// <summary>
        /// The time this message was sent from the client.
        /// </summary>
        public const string TimeSent = "NServiceBus.TimeSent";

        /// <summary>
        /// Id of the message that caused this message to be sent.
        /// </summary>
        public const string RelatedTo = "NServiceBus.RelatedTo";

        /// <summary>
        /// Header entry key indicating the types of messages contained.
        /// </summary>
        public const string EnclosedMessageTypes = "NServiceBus.EnclosedMessageTypes";

        /// <summary>
        /// Header entry key indicating format of the payload.
        /// </summary>
        public const string ContentType = "NServiceBus.ContentType";

        /// <summary>
        /// Header entry key for the given message type that is being subscribed to, when message intent is subscribe or
        /// unsubscribe.
        /// </summary>
        public const string SubscriptionMessageType = "SubscriptionMessageType";

        /// <summary>
        /// Header entry key for the transport address of the subscribing endpoint.
        /// </summary>
        public const string SubscriberTransportAddress = "NServiceBus.SubscriberAddress";

        /// <summary>
        /// Header entry key for the logical name of the subscribing endpoint.
        /// </summary>
        public const string SubscriberEndpoint = "NServiceBus.SubscriberEndpoint";

        /// <summary>
        /// True if this message is a saga timeout.
        /// </summary>
        public const string IsSagaTimeoutMessage = "NServiceBus.IsSagaTimeoutMessage";

        /// <summary>
        /// True if this is a deferred message.
        /// </summary>
        public const string IsDeferredMessage = "NServiceBus.IsDeferredMessage";

        /// <summary>
        /// Name of the endpoint where the given message originated.
        /// </summary>
        public const string OriginatingEndpoint = "NServiceBus.OriginatingEndpoint";

        /// <summary>
        /// Machine name of the endpoint where the given message originated.
        /// </summary>
        public const string OriginatingMachine = "NServiceBus.OriginatingMachine";

        /// <summary>
        /// HostId of the endpoint where the given message originated.
        /// </summary>
        public const string OriginatingHostId = "$.diagnostics.originating.hostid";

        /// <summary>
        /// Name of the endpoint where the given message was processed (success or failure).
        /// </summary>
        public const string ProcessingEndpoint = "NServiceBus.ProcessingEndpoint";

        /// <summary>
        /// Machine name of the endpoint where the given message was processed (success or failure).
        /// </summary>
        public const string ProcessingMachine = "NServiceBus.ProcessingMachine";

        /// <summary>
        /// The display name of the host where the given message was processed (success or failure), eg the MachineName.
        /// </summary>
        public const string HostDisplayName = "$.diagnostics.hostdisplayname";

        /// <summary>
        /// HostId of the endpoint where the given message was processed (success or failure).
        /// </summary>
        public const string HostId = "$.diagnostics.hostid";

        /// <summary>
        /// HostId of the endpoint where the given message was processed (success or failure).
        /// </summary>
        public const string HasLicenseExpired = "$.diagnostics.license.expired";

        /// <summary>
        /// The original reply to address for successfully processed messages.
        /// </summary>
        public const string OriginatingAddress = "NServiceBus.OriginatingAddress";

        /// <summary>
        /// The id of the message conversation that this message is part of.
        /// </summary>
        public const string ConversationId = "NServiceBus.ConversationId";

        /// <summary>
        /// The intent of the current message.
        /// </summary>
        public const string MessageIntent = "NServiceBus.MessageIntent";

        /// <summary>
        /// The identifier to lookup the key to decrypt the encrypted data.
        /// </summary>
        public const string RijndaelKeyIdentifier = "NServiceBus.RijndaelKeyIdentifier";

        /// <summary>
        /// The time to be received for this message when it was sent the first time.
        /// When moved to error and audit this header will be preserved to the original TTBR
        /// of the message can be known.
        /// </summary>
        public const string TimeToBeReceived = "NServiceBus.TimeToBeReceived";

        /// <summary>
        /// Indicates that the message was sent as a non-durable message.
        /// </summary>
        public const string NonDurableMessage = "NServiceBus.NonDurableMessage";
    }
}