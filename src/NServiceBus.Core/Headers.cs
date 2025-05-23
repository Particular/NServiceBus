﻿namespace NServiceBus;

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
    /// </summary>
    public const string RouteTo = "NServiceBus.Header.RouteTo";

    /// <summary>
    /// Header for specifying to which sites the gateway should send the message. For multiple
    /// sites, a comma separated list can be used.
    /// </summary>
    public const string DestinationSites = "NServiceBus.DestinationSites";

    /// <summary>
    /// Header for specifying the key for the site where this message originated.
    /// </summary>
    public const string OriginatingSite = "NServiceBus.OriginatingSite";

    /// <summary>
    /// Header containing the id of the saga instance that sent the message.
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

    /// <summary>
    /// The time the last Delayed Retry has been performed for this message.
    /// </summary>
    public const string DelayedRetriesTimestamp = "NServiceBus.Retries.Timestamp";

    /// <summary>
    /// The number of Immediate Retries that have been performed for this message.
    /// </summary>
    public const string ImmediateRetries = "NServiceBus.FLRetries";

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
    /// The time this message should be delivered to the endpoint to start processing.
    /// </summary>
    public const string DeliverAt = "NServiceBus.DeliverAt";

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
    /// Indicates if the license used by the processing endpoint has expired.
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
    /// The id of the previous message conversation that triggered this message.
    /// </summary>
    public const string PreviousConversationId = "NServiceBus.PreviousConversationId";

    /// <summary>
    /// The intent of the current message.
    /// </summary>
    public const string MessageIntent = "NServiceBus.MessageIntent";

    /// <summary>
    /// Indicates that the message was sent as a non-durable message.
    /// </summary>
    public const string NonDurableMessage = "NServiceBus.NonDurableMessage";

    /// <summary>
    /// The time to be received for this message when it was sent the first time.
    /// When moved to error and audit this header will be preserved to the original TTBR
    /// of the message can be known.
    /// </summary>
    public const string TimeToBeReceived = "NServiceBus.TimeToBeReceived";

    /// <summary>
    /// Traceparent header according to the W3C spec:
    /// https://www.w3.org/TR/trace-context/#traceparent-header
    /// 23 November 2021.
    /// </summary>
    public const string DiagnosticsTraceParent = "traceparent";

    /// <summary>
    /// Tracestate header according to the W3C spec:
    /// https://www.w3.org/TR/trace-context/#tracestate-header
    /// 23 November 2021.
    /// </summary>
    public const string DiagnosticsTraceState = "tracestate";

    /// <summary>
    /// Baggage header according to the W3C spec:
    /// https://www.w3.org/TR/baggage/#baggage-http-header-format
    /// 8 June 2022.
    /// </summary>
    public const string DiagnosticsBaggage = "baggage";

    /// <summary>
    /// The content type used to serialize the data bus properties in the message.
    /// </summary>
    // This is now defined in the ClaimCheck package, but is being kept here because it's referenced by ActivityDecorator to be able to promote the header to an activity tag
    public const string DataBusConfigContentType = "NServiceBus.DataBusConfig.ContentType"; // NOTE: .DataConfig required for compatibility with the Gateway BLOB matching behavior.

    /// <summary>
    /// This header is set when a new trace should be started when receiving this message.
    /// This is automatically set when: a saga timeout is requested, a message is set to be delivered at a certain time, a delayed retry is requested or a message is moved to the error queue.
    /// </summary>
    public const string StartNewTrace = "NServiceBus.OpenTelemetry.StartNewTrace";
}
