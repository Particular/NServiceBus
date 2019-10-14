namespace NServiceBus
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Do not rename the methods or parameters on this class nor change the event declarations because it directly affects
    /// the events that are generated.
    /// </summary>
    /// <remarks>
    /// Operations here are used on on hot path where async state machine was optimized away. Enable check done as
    /// part of caller
    /// </remarks>
    [EventSource(Name = EventSourceName)]
    sealed class MessageOperationsEventSource : EventSource
    {
        MessageOperationsEventSource()
        {
        }

        [Event(SendStartEventId, Message = "Sending message with MessageId '{0}' started.", Level = EventLevel.Informational)]
        public void SendStart(string MessageId) => WriteEvent(SendStartEventId, MessageId);

        [Event(SendStopEventId, Message = "Sending message with MessageId '{0}' stopped.", Level = EventLevel.Informational)]
        public void SendStop(string MessageId, bool IsFaulted) => WriteEvent(SendStopEventId, MessageId, IsFaulted);

        [Event(PublishStartEventId, Message = "Publishing message with MessageId '{0}' started.", Level = EventLevel.Informational)]
        public void PublishStart(string MessageId) => WriteEvent(PublishStartEventId, MessageId);

        [Event(PublishStopEventId, Message = "Publishing message with MessageId '{0}' stopped.", Level = EventLevel.Informational)]
        public void PublishStop(string MessageId, bool IsFaulted) => WriteEvent(PublishStopEventId, MessageId, IsFaulted);

        [Event(ReplyStartEventId, Message = "Replying message with MessageId '{0}' started.", Level = EventLevel.Informational)]
        public void ReplyStart(string MessageId) => WriteEvent(ReplyStartEventId, MessageId);

        [Event(ReplyStopEventId, Message = "Replying message with MessageId '{0}' stopped.", Level = EventLevel.Informational)]
        public void ReplyStop(string MessageId, bool IsFaulted) => WriteEvent(ReplyStopEventId, MessageId, IsFaulted);

        // optimized version for the common signature
        [NonEvent]
        unsafe void WriteEvent(int EventId, string MessageId, bool IsFaulted)
        {
            fixed(char* messageIdPtr = MessageId)
            {
                var eventPayload = stackalloc EventData[2];

                eventPayload[0].Size = (MessageId.Length + 1) * 2;
                eventPayload[0].DataPointer = (IntPtr)messageIdPtr;

                eventPayload[1].Size = sizeof(bool);
                eventPayload[1].DataPointer = (IntPtr)(&IsFaulted);
                WriteEventCore(EventId, 2, eventPayload);
            }
        }

        const string EventSourceName = "NServiceBus.Messages";
        const int SendStartEventId = 1;
        const int SendStopEventId = 2;
        const int PublishStartEventId = 3;
        const int PublishStopEventId = 4;
        const int ReplyStartEventId = 5;
        const int ReplyStopEventId = 6;

        internal static readonly MessageOperationsEventSource Log = new MessageOperationsEventSource();
    }
}