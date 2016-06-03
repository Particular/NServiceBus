namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Allows steps to be registered in order.
    /// </summary>
    public partial class StepRegistrationSequence
    {
        internal StepRegistrationSequence(Action<RegisterStep> addStep)
        {
            this.addStep = addStep;
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="Behavior{TContext}" /> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            var step = RegisterStep.Create(stepId, behavior, description);
            addStep(step);
            return this;
        }

        Action<RegisterStep> addStep;
    }
}