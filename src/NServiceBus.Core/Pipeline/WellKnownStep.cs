#pragma warning disable 1591
namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Well known steps.
    /// </summary>
    public class WellKnownStep
    {
        readonly string stepId;

        WellKnownStep(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new InvalidOperationException("PipelineStep cannot be empty string or null. Use a valid name instead.");
            }
            this.stepId = stepId;
        }


        internal static WellKnownStep Create(string customStepId)
        {
            return new WellKnownStep(customStepId);
        }

        public static implicit operator string(WellKnownStep step)
        {
            Guard.AgainstNull(step, "step");
            return step.stepId;
        }

        /// <summary>
        /// Host information
        /// </summary>
        public static WellKnownStep HostInformation = new WellKnownStep("HostInformation");
        /// <summary>
        /// Statistics analysis
        /// </summary>
        public static WellKnownStep ProcessingStatistics = new WellKnownStep("ProcessingStatistics");
        /// <summary>
        /// Auditing
        /// </summary>
        public static readonly WellKnownStep AuditProcessedMessage = new WellKnownStep("AuditProcessedMessage");
        /// <summary>
        /// Child Container creator.
        /// </summary>
        public static readonly WellKnownStep CreateChildContainer = new WellKnownStep("CreateChildContainer");
        /// <summary>
        /// Executes UoWs.
        /// </summary>
        public static readonly WellKnownStep ExecuteUnitOfWork = new WellKnownStep("ExecuteUnitOfWork");
        /// <summary>
        /// Runs incoming mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public static readonly WellKnownStep MutateIncomingTransportMessage = new WellKnownStep("MutateIncomingTransportMessage");
        /// <summary>
        /// Send messages to the wire.
        /// </summary>
        public static readonly WellKnownStep DispatchMessageToTransport = new WellKnownStep("DispatchMessageToTransport");
        /// <summary>
        /// Invokes IHandleMessages{T}.Handle(T)
        /// </summary>
        public static readonly WellKnownStep InvokeHandlers = new WellKnownStep("InvokeHandlers");
        /// <summary>
        /// Runs incoming mutation for each logical message.
        /// </summary>
        public static readonly WellKnownStep MutateIncomingMessages = new WellKnownStep("MutateIncomingMessages");
        /// <summary>
        /// Invokes the saga code.
        /// </summary>
        public static readonly WellKnownStep InvokeSaga = new WellKnownStep("InvokeSaga");
        /// <summary>
        /// Ensures best practices are met.
        /// </summary>
        public static readonly WellKnownStep EnforceBestPractices = new WellKnownStep("EnforceBestPractices");
        /// <summary>
        /// Runs outgoing mutation for each logical message.
        /// </summary>
        public static readonly WellKnownStep MutateOutgoingMessages = new WellKnownStep("MutateOutgoingMessages");

        /// <summary>
        /// Runs outgoing mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public static readonly WellKnownStep MutateOutgoingTransportMessage = new WellKnownStep("MutateOutgoingTransportMessage");
    }
}
