namespace NServiceBus.Pipeline
{
    using System;
    using ObjectBuilder;

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

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="factoryMethod">A callback that creates the behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register<T>(string stepId, Func<IBuilder, T> factoryMethod, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");

            var step = RegisterStep.Create(stepId, typeof(T), description, b => factoryMethod(b));
            addStep(step);
            return this;
        }

        Action<RegisterStep> addStep;
    }
}