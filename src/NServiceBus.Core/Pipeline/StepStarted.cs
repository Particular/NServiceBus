namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Step execution started.
    /// </summary>
    public struct StepStarted
    {
        readonly string pipeId;
        readonly string stepId;
        readonly Type behavior;

        /// <summary>
        /// Creates an instance of <see cref="StepStarted"/>.
        /// </summary>
        /// <param name="pipeId">Pipe identifier.</param>
        /// <param name="stepId">Step identifier.</param>
        /// <param name="behavior">Behavior type.</param>
        public StepStarted(string pipeId, string stepId, Type behavior)
        {
            this.pipeId = pipeId;
            this.stepId = stepId;
            this.behavior = behavior;
        }

        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get { return pipeId; } }

        /// <summary>
        /// Step identifier.
        /// </summary>
        public string StepId { get { return stepId; } }

        /// <summary>
        /// Behavior type.
        /// </summary>
        public Type Behavior { get { return behavior; } }
    }
}