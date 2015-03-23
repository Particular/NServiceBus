namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    ///     Step execution started.
    /// </summary>
    public struct StepStarted
    {
        /// <summary>
        ///     Creates an instance of <see cref="StepStarted" />.
        /// </summary>
        /// <param name="stepId">Step identifier.</param>
        /// <param name="behavior">Behavior type.</param>
        /// <param name="stepEnded">Observable for when step ends.</param>
        public StepStarted(string stepId, Type behavior, IObservable<StepEnded> stepEnded)
        {
            Guard.AgainstNullAndEmpty(stepId, "stepId");
            Guard.AgainstNull(behavior, "behavior");
            Guard.AgainstNull(stepEnded, "stepEnded");
            this.stepId = stepId;
            this.behavior = behavior;
            this.stepEnded = stepEnded;
        }

        /// <summary>
        ///     Behavior type.
        /// </summary>
        public Type Behavior
        {
            get { return behavior; }
        }

        /// <summary>
        ///     Step identifier.
        /// </summary>
        public string StepId
        {
            get { return stepId; }
        }

        /// <summary>
        /// Step ended.
        /// </summary>
        public IObservable<StepEnded> Ended
        {
            get { return stepEnded; }
        }

        readonly Type behavior;
        readonly IObservable<StepEnded> stepEnded;
        readonly string stepId;
    }
}