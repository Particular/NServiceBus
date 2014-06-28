#pragma warning disable 1591
namespace NServiceBus.Pipeline
{
    using System;
    using Unicast.Messages;

    /// <summary>
    /// Well known <see cref="IBehavior{TContext}"/> types.
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

        internal static WellKnownStep CreateCustom(string customStepId)
        {
            return new WellKnownStep(customStepId);
        }

        public static implicit operator string(WellKnownStep step)
        {
            return step.stepId;
        }


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
        /// Deserializes all logical messages from the transport message.
        /// </summary>
        public static readonly WellKnownStep DeserializeMessages = new WellKnownStep("DeserializeMessages");
        /// <summary>
        /// Runs incoming mutation for each logical message.
        /// </summary>
        public static readonly WellKnownStep MutateIncomingMessages = new WellKnownStep("MutateIncomingMessages");
        /// <summary>
        /// Loads all handlers to be executed.
        /// </summary>
        public static readonly WellKnownStep LoadHandlers = new WellKnownStep("LoadHandlers");
        /// <summary>
        /// Invokes the saga code.
        /// </summary>
        public static readonly WellKnownStep InvokeSaga = new WellKnownStep("InvokeSaga");
        /// <summary>
        /// Loops through all <see cref="LogicalMessage"/>.
        /// </summary>
        public static readonly WellKnownStep ExecuteLogicalMessages = new WellKnownStep("ExecuteLogicalMessages");
        /// <summary>
        /// Ensures best practices are met.
        /// </summary>
        public static readonly WellKnownStep EnforceBestPractices = new WellKnownStep("EnforceBestPractices");
        /// <summary>
        /// Runs outgoing mutation for each logical message.
        /// </summary>
        public static readonly WellKnownStep MutateOutgoingMessages = new WellKnownStep("MutateOutgoingMessages");
        /// <summary>
        /// Creates the protocol messages to send out.
        /// </summary>
        public static readonly WellKnownStep CreatePhysicalMessage = new WellKnownStep("CreatePhysicalMessage");
        /// <summary>
        /// Serializes messages.
        /// </summary>
        public static readonly WellKnownStep SerializeMessage = new WellKnownStep("SerializeMessage");
        /// <summary>
        /// Runs outgoing mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public static readonly WellKnownStep MutateOutgoingTransportMessage = new WellKnownStep("MutateOutgoingTransportMessage");
    }
}
