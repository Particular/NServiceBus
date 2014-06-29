namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Step execution started.
    /// </summary>
    public struct StepStarted
    {
        /// <summary>
        /// Pipe identifier. 
        /// </summary>
        public string PipeId { get; set; }

        /// <summary>
        /// Step identifier.
        /// </summary>
        public string StepId { get; internal set; }

        /// <summary>
        /// Behavior type.
        /// </summary>
        public Type Behavior { get; internal set; }
    }
}