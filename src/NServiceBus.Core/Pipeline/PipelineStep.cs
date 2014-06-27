#pragma warning disable 1591
namespace NServiceBus.Pipeline
{
    using System;
    using Unicast.Messages;

    /// <summary>
    /// Well known <see cref="IBehavior{TContext}"/> types.
    /// </summary>
    public class PipelineStep
    {
        readonly string stepId;

        PipelineStep(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                throw new InvalidOperationException("PipelineStep cannot be empty string or null. Use a valid name instead.");
            }
            this.stepId = stepId;
        }

        public static PipelineStep CreateCustom(string customStepId)
        {
            return new PipelineStep(customStepId);
        }

        public static implicit operator string(PipelineStep step)
        {
            return step.stepId;
        }


        /// <summary>
        /// Auditing
        /// </summary>
        public static readonly PipelineStep AuditProcessedMessage = new PipelineStep("AuditProcessedMessage");
        /// <summary>
        /// Child Container creator.
        /// </summary>
        public static readonly PipelineStep CreateChildContainer = new PipelineStep("CreateChildContainer");
        /// <summary>
        /// Executes UoWs.
        /// </summary>
        public static readonly PipelineStep ExecuteUnitOfWork = new PipelineStep("ExecuteUnitOfWork");
        /// <summary>
        /// Runs incoming mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public static readonly PipelineStep MutateIncomingTransportMessage = new PipelineStep("MutateIncomingTransportMessage");
        /// <summary>
        /// Send messages to the wire.
        /// </summary>
        public static readonly PipelineStep DispatchMessageToTransport = new PipelineStep("DispatchMessageToTransport");
        /// <summary>
        /// Invokes IHandleMessages{T}.Handle(T)
        /// </summary>
        public static readonly PipelineStep InvokeHandlers = new PipelineStep("InvokeHandlers");
        /// <summary>
        /// Deserializes all logical messages from the transport message.
        /// </summary>
        public static readonly PipelineStep DeserializeMessages = new PipelineStep("DeserializeMessages");
        /// <summary>
        /// Runs incoming mutation for each logical message.
        /// </summary>
        public static readonly PipelineStep MutateIncomingMessages = new PipelineStep("MutateIncomingMessages");
        /// <summary>
        /// Loads all handlers to be executed.
        /// </summary>
        public static readonly PipelineStep LoadHandlers = new PipelineStep("LoadHandlers");
        /// <summary>
        /// Invokes the saga code.
        /// </summary>
        public static readonly PipelineStep InvokeSaga = new PipelineStep("InvokeSaga");
        /// <summary>
        /// Loops through all <see cref="LogicalMessage"/>.
        /// </summary>
        public static readonly PipelineStep ExecuteLogicalMessages = new PipelineStep("ExecuteLogicalMessages");
        /// <summary>
        /// Ensures best practices are met.
        /// </summary>
        public static readonly PipelineStep EnforceBestPractices = new PipelineStep("EnforceBestPractices");
        /// <summary>
        /// Runs outgoing mutation for each logical message.
        /// </summary>
        public static readonly PipelineStep MutateOutgoingMessages = new PipelineStep("MutateOutgoingMessages");
        /// <summary>
        /// Creates the protocol messages to send out.
        /// </summary>
        public static readonly PipelineStep CreatePhysicalMessage = new PipelineStep("CreatePhysicalMessage");
        /// <summary>
        /// Serializes messages.
        /// </summary>
        public static readonly PipelineStep SerializeMessage = new PipelineStep("SerializeMessage");
        /// <summary>
        /// Runs outgoing mutation for <see cref="TransportMessage"/>.
        /// </summary>
        public static readonly PipelineStep MutateOutgoingTransportMessage = new PipelineStep("MutateOutgoingTransportMessage");
    }
}
