namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Allows steps to be registered in order.
    /// </summary>
    public class StepRegistrationSequence
    {
        readonly Action<RegisterStep> addStep;

        internal StepRegistrationSequence(Action<RegisterStep> addStep)
        {
            this.addStep = addStep;
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="Behavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        /// <param name="isStatic">Is this behavior pipeline-static</param>
        public StepRegistrationSequence Register(string stepId, Type behavior, string description, bool isStatic = false)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            Guard.AgainstNullAndEmpty(stepId, "stepId");
            Guard.AgainstNullAndEmpty(description, "description");

            var step = RegisterStep.Create(stepId, behavior, description, isStatic);
            addStep(step);
            return this;
        }


        /// <summary>
        /// <see cref="Register(string,System.Type,string, bool)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="Behavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        /// <param name="isStatic">Is this behavior pipeline-static</param>
        public StepRegistrationSequence Register(WellKnownStep wellKnownStep, Type behavior, string description, bool isStatic = false)
        {
            Guard.AgainstNull(wellKnownStep, "wellKnownStep");

            Register((string)wellKnownStep, behavior, description, isStatic);
            return this;
        }
    }
}