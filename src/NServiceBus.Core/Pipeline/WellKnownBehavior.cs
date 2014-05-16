namespace NServiceBus.Pipeline
{
    public static class WellKnownBehavior
    {
        public const string AuditForwarder = "Audit";
        public const string ChildContainer = "ChildContainer";
        public const string UnitOfWork = "UnitOfWork";
        public const string IncomingTransportMessageMutators = "IncomingTransportMessageMutators";
        public const string DispatchMessageToTransport = "DispatchMessageToTransport";
        public const string InvokeHandlers = "InvokeHandlers";
        public const string ExtractLogicalMessages = "ExtractLogicalMessages";
    }
}