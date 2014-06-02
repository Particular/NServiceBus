#pragma warning disable 1591
namespace NServiceBus.Pipeline
{
    using Unicast.Messages;

    /// <summary>
    /// Well known <see cref="IBehavior{TContext}"/> types.
    /// </summary>
    public static class WellKnownBehavior
    {
        /// <summary>
        /// Auditing.
        /// </summary>
        public const string Audit = "Audit";
        /// <summary>
        /// Child Container creator.
        /// </summary>
        public const string ChildContainer = "ChildContainer";
        /// <summary>
        /// Executes UoWs.
        /// </summary>
        public const string UnitOfWork = "UnitOfWork";
        /// <summary>
        /// Runs incoming mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public const string MutateIncomingTransportMessage = "MutateIncomingTransportMessage";
        /// <summary>
        /// Send messages to the wire.
        /// </summary>
        public const string DispatchMessageToTransport = "DispatchMessageToTransport";
        /// <summary>
        /// Invokes IHandleMessages{T}.Handle(T)
        /// </summary>
        public const string InvokeHandlers = "InvokeHandlers";
        /// <summary>
        /// Extracts all logical messages from the transport message.
        /// </summary>
        public const string ExtractLogicalMessages = "ExtractLogicalMessages";
        /// <summary>
        /// Runs incoming mutation for each logical message.
        /// </summary>
        public const string MutateIncomingMessages = "MutateIncomingMessages";
        /// <summary>
        /// Loads all handlers to be executed.
        /// </summary>
        public const string LoadHandlers = "ExecuteHandlers";
        /// <summary>
        /// Invokes the saga code.
        /// </summary>
        public const string InvokeSaga = "InvokeSaga";
        /// <summary>
        /// Loops through all <see cref="LogicalMessage"/>.
        /// </summary>
        public const string ExecuteLogicalMessages = "ExecuteLogicalMessages";
        /// <summary>
        /// Ensures best practices are met.
        /// </summary>
        public const string EnforceBestPractices = "EnforceBestPractices";
        /// <summary>
        /// Runs outgoing mutation for each logical message.
        /// </summary>
        public const string MutateOutgoingMessages = "MutateOutgoingMessages";
        /// <summary>
        /// Creates the protocol messages to send out.
        /// </summary>
        public const string CreatePhysicalMessage = "CreatePhysicalMessage";
        /// <summary>
        /// Serializes messages.
        /// </summary>
        public const string SerializeMessage = "SerializeMessage";
        /// <summary>
        /// Runs outgoing mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public const string MutateOutgoingTransportMessage = "MutateOutgoingTransportMessage";
    }
}
