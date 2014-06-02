#pragma warning disable 1591
namespace NServiceBus.Pipeline
{
    /// <summary>
    /// A list of commonly used keys that map to <see cref="IBehavior{TContext}"/> functionality.
    /// </summary>
    public static class WellKnownBehavior
    {
        public const string Audit = "Audit";
        public const string ChildContainer = "ChildContainer";
        public const string UnitOfWork = "UnitOfWork";
        public const string MutateIncomingTransportMessage = "MutateIncomingTransportMessage";
        public const string DispatchMessageToTransport = "DispatchMessageToTransport";
        public const string InvokeHandlers = "InvokeHandlers";
        public const string ExtractLogicalMessages = "ExtractLogicalMessages";
        public const string MutateIncomingMessages = "MutateIncomingMessages";
        public const string ExecuteHandlers = "ExecuteHandlers";
        public const string InvokeSaga = "InvokeSaga";
        public const string ExecuteLogicalMessages = "ExecuteLogicalMessages";
        public const string EnforceBestPractices = "EnforceBestPractices";
        public const string MutateOutgoingMessages = "MutateOutgoingMessages";
        public const string CreatePhysicalMessage = "CreatePhysicalMessage";
        public const string SerializeMessage = "SerializeMessage";
        public const string MutateOutgoingTransportMessage = "MutateOutgoingTransportMessage";
    }
}