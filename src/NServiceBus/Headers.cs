namespace NServiceBus
{
    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static class Headers
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
        /// Header for time when a message expires in the timeout manager
        /// This header is considered an applicative header.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0", Message = "This header is only populated by the timeoutmanager based deferral mechanism and will not be used in future versions")]
        public const string Expire = "NServiceBus.Timeout.Expire";

        /// <summary>
        /// Header for redirecting the expired timeout to a endpoint other than the one setting the Timeout
        /// This header is considered an applicative header.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0", Message = "This header is only populated by the timeoutmanager based deferral mechanism and will not be used in future versions")]
        public const string RouteExpiredTimeoutTo = "NServiceBus.Timeout.RouteExpiredTimeoutTo";

        /// <summary>
        /// Header containing the id of the saga instance the sent the message
        /// This header is considered an applicative header.
        /// </summary>
        public const string SagaId = "NServiceBus.SagaId";

        /// <summary>
        /// Header telling the timeout manager to clear previous timeouts
        /// This header is considered an applicative header.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0", Message = "This header is only populated by the timeoutmanager based deferral mechanism and will not be used in future versions")]
        public const string ClearTimeouts = "NServiceBus.ClearTimeouts";

        /// <summary>
        /// Prefix included on the wire when sending applicative headers.
        /// </summary>
        public const string HeaderName = "Header";

        /// <summary>
        /// Header containing the windows identity name
        /// </summary>
        public const string WindowsIdentityName = "WinIdName";

        /// <summary>
        /// Header telling the NServiceBus Version (beginning NServiceBus V3.0.1).
        /// </summary>
        public const string NServiceBusVersion = "NServiceBus.Version";

        /// <summary>
        /// Used in a header when doing a callback (bus.return)
        /// </summary>
        public const string ReturnMessageErrorCodeHeader = "NServiceBus.ReturnMessage.ErrorCode";

        /// <summary>
        /// Header that tells if this transport message is a control message
        /// </summary>
        public const string ControlMessageHeader = "NServiceBus.ControlMessage";

        /// <summary>
        /// Type of the saga that this message is targeted for
        /// </summary>
        public const string SagaType = "NServiceBus.SagaType";

        /// <summary>
        /// Type of the saga that sent this message
        /// </summary>
        [ObsoleteEx(Replacement = "SagaType", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public const string SagaEntityType = "NServiceBus.SagaDataType";

        /// <summary>
        /// Id of the saga that sent this message
        /// </summary>
        public const string OriginatingSagaId = "NServiceBus.OriginatingSagaId";

        /// <summary>
        /// Type of the saga that sent this message
        /// </summary>
        public const string OriginatingSagaType = "NServiceBus.OriginatingSagaType";

        /// <summary>
        /// The number of retries that has been performed for this message
        /// </summary>
        public const string Retries = "NServiceBus.Retries";

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

        /// <summary>
        /// Header entry key indicating format of the payload
        /// </summary>
        public const string ContentType = "NServiceBus.ContentType";

        /// <summary>
        /// Used for correlation id message.
        /// </summary>
        [ObsoleteEx(Message = "The IdForCorrelation header is replaced by Id", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public const string IdForCorrelation = "CorrId";

        /// <summary>
        /// Header entry key for the given message type that is being subscribed to, when message intent is subscribe or unsubscribe.
        /// </summary>
        public const string SubscriptionMessageType = "SubscriptionMessageType";

        /// <summary>
        /// Header key for setting/getting the ID of the message as it was when it failed processing.
        /// </summary>
        [ObsoleteEx(Message = "The OriginalId headers is replaced by the IdForCorrelation header since they are now equivalent", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public const string OriginalId = "NServiceBus.OriginalId";

        /// <summary>
        /// True if this message is a saga timeout
        /// </summary>
        public const string IsSagaTimeoutMessage = "NServiceBus.IsSagaTimeoutMessage";

        /// <summary>
        /// True if this is a deferred message
        /// </summary>
        public const string IsDeferredMessage = "NServiceBus.IsDeferredMessage";

        /// <summary>
        /// Name of the endpoint where the given message originated
        /// </summary>
        public const string OriginatingEndpoint = "NServiceBus.OriginatingEndpoint";

        /// <summary>
        /// Machine name of the endpoint where the given message originated
        /// </summary>
        public const string OriginatingMachine = "NServiceBus.OriginatingMachine";

        /// <summary>
        /// Name of the endpoint where the given message was processed(success or failure)
        /// </summary>
        public const string ProcessingEndpoint = "NServiceBus.ProcessingEndpoint";

        /// <summary>
        /// Machine name of the endpoint where the given message was processed(success or failure)
        /// </summary>
        public const string ProcessingMachine = "NServiceBus.ProcessingMachine";

        /// <summary>
        /// The original reply to address for successfully processed messages
        /// </summary>
        public const string OriginatingAddress = "NServiceBus.OriginatingAddress";

        /// <summary>
        /// The id of the message conversation that this message is part of
        /// </summary>
        public const string ConversationId = "NServiceBus.ConversationId";

        /// <summary>
        /// The intent of the current message
        /// </summary>
        public const string MessageIntent = "NServiceBus.MessageIntent";

        /// <summary>
        /// Get the header with the given key. Cannot be used to change its value.
        /// </summary>
        /// <param name="msg">The message to retrieve a header from.</param>
        /// <param name="key">The header key.</param>
        /// <returns>The value assigned to the header.</returns>
        public static string GetMessageHeader(object msg, string key)
        {
            return ExtensionMethods.GetHeaderAction(msg, key);
        }

        /// <summary>
        /// Sets the value of the header for the given key.
        /// </summary>
        /// <param name="msg">The message to add a header to.</param>
        /// <param name="key">The header key.</param>
        /// <param name="value">The value to assign to the header.</param>
        public static void SetMessageHeader(object msg, string key, string value)
        {
            ExtensionMethods.SetHeaderAction(msg, key, value);
        }
    }
}
