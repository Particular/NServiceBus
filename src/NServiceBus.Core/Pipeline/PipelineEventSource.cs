namespace NServiceBus
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Do not rename the methods or parameters on this class nor change the event declarations because it directly affects
    /// the events that are generated.
    /// </summary>
    [EventSource(Name = EventSourceName)]
    [SuppressMessage("ReSharper", "ParameterHidesMember")]
    sealed class PipelineEventSource : EventSource
    {
        PipelineEventSource()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(MainPipelineStartEventId, Message = "Main Pipeline for MessageId '{0}' started.", Level = EventLevel.Informational)]
        public void MainStart(string MessageId) // used within already existing state machine. Enabled check done as part of method implementation
        {
            if (IsEnabled())
            {
                WriteEvent(MainPipelineStartEventId, MessageId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(MainPipelineStopEventId, Message = "Main Pipeline for MessageId '{0}' stopped.", Level = EventLevel.Informational)]
        public unsafe void MainStop(string MessageId, bool IsFaulted) // used within already existing state machine. Enabled check done as part of method implementation
        {
            if (IsEnabled())
            {
                fixed(char* messageIdPtr = MessageId)
                {
                    var eventPayload = stackalloc EventData[2];

                    eventPayload[0].Size = (MessageId.Length + 1) * 2;
                    eventPayload[0].DataPointer = (IntPtr)messageIdPtr;

                    eventPayload[1].Size = sizeof(bool);
                    eventPayload[1].DataPointer = (IntPtr)(&IsFaulted);
                    WriteEventCore(MainPipelineStopEventId, 2, eventPayload);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(SatellitePipelineStartEventId, Message = "Satellite Pipeline '{0}' for MessageId '{1}' started.", Level = EventLevel.Informational)]
        public void SatelliteStart(string Name, string MessageId) // used on on hot path where async state machine was optimized away. Enable check done as part of caller
        {
            WriteEvent(SatellitePipelineStartEventId, Name, MessageId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(SatellitePipelineStopEventId, Message = "Satellite Pipeline '{0}' for MessageId '{1}' stopped.", Level = EventLevel.Informational)]
        public unsafe void SatelliteStop(string Name, string MessageId, bool IsFaulted) // used on on hot path where async state machine was optimized away. Enable check done as part of caller
        {
            fixed(char* namePtr = Name)
            fixed(char* messageIdPtr = MessageId)
            {
                var eventPayload = stackalloc EventData[3];

                eventPayload[0].Size = (Name.Length + 1) * 2;
                eventPayload[0].DataPointer = (IntPtr)namePtr;

                eventPayload[1].Size = (MessageId.Length + 1) * 2;
                eventPayload[1].DataPointer = (IntPtr)messageIdPtr;

                eventPayload[2].Size = sizeof(bool);
                eventPayload[2].DataPointer = (IntPtr)(&IsFaulted);
                WriteEventCore(SatellitePipelineStopEventId, 3, eventPayload);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(PipelineStartEventId, Message = "Pipeline '{0}' started.'", Level = EventLevel.Verbose
#if NETSTANDARD
            , ActivityOptions = EventActivityOptions.Recursive // Pipelines are started within pipeline, to avoid auto-stop we need recursive on the platform it is available
#endif
        )]
        public void InvokeStart(string Name) // used on on hot path where async state machine was optimized away. Enable check done as part of caller
        {
            WriteEvent(PipelineStartEventId, Name);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(PipelineStopEventId, Message = "Pipeline '{0}' stopped.'", Level = EventLevel.Verbose)]
        public unsafe void InvokeStop(string Name, bool IsFaulted) // used on on hot path where async state machine was optimized away. Enable check done as part of caller
        {
            fixed(char* namePtr = Name)
            {
                var eventPayload = stackalloc EventData[2];

                eventPayload[0].Size = (Name.Length + 1) * 2;
                eventPayload[0].DataPointer = (IntPtr)namePtr;

                eventPayload[1].Size = sizeof(bool);
                eventPayload[1].DataPointer = (IntPtr)(&IsFaulted);
                WriteEventCore(PipelineStopEventId, 2, eventPayload);
            }
        }

        const string EventSourceName = "NServiceBus.Pipeline";
        const int MainPipelineStartEventId = 1;
        const int MainPipelineStopEventId = 2;
        const int PipelineStartEventId = 3;
        const int PipelineStopEventId = 4;
        const int SatellitePipelineStartEventId = 5;
        const int SatellitePipelineStopEventId = 6;

        internal static readonly PipelineEventSource Log = new PipelineEventSource();
    }
}