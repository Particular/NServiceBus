namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Manages the pipeline configuration.
    /// </summary>
    public class PipelineSettings
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PipelineSettings"/>.
        /// </summary>
        internal PipelineSettings(PipelineModifications modifications)
        {
            this.modifications = modifications;
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the step to remove.</param>
        public void Remove(string stepId)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);

            modifications.Removals.Add(new RemoveStep(stepId));
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to remove.</param>
        public void Remove(WellKnownStep wellKnownStep)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            Guard.AgainstNull(nameof(wellKnownStep), wellKnownStep);

            Remove((string)wellKnownStep);
        }

        /// <summary>
        /// Replaces an existing step behavior with a new one.
        /// </summary>
        /// <param name="stepId">The identifier of the step to replace its implementation.</param>
        /// <param name="newBehavior">The new <see cref="Behavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(string stepId, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");
            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);

            registeredBehaviors.Add(newBehavior);
            modifications.Replacements.Add(new ReplaceStep(stepId, newBehavior, description));
        }

        /// <summary>
        /// <see cref="Replace(string,System.Type,string)"/>.
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to replace.</param>
        /// <param name="newBehavior">The new <see cref="Behavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(WellKnownStep wellKnownStep, Type newBehavior, string description = null)
        {
            Guard.AgainstNull(nameof(wellKnownStep), wellKnownStep);

            Replace((string)wellKnownStep, newBehavior, description);
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="Behavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            AddStep(RegisterStep.Create(stepId, behavior, description));
            return new StepRegistrationSequence(AddStep);
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="factoryMethod">A callback that creates the behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register<T>(string stepId, Func<IChildBuilder, T> factoryMethod, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            AddStep(RegisterStep.Create(stepId, typeof(T), description, b => factoryMethod(b)));
            return new StepRegistrationSequence(AddStep);
        } 
        
        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register<T>(string stepId, T behavior, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            AddStep(RegisterStep.Create(stepId, typeof(T), description, _ => behavior));
            return new StepRegistrationSequence(AddStep);
        }

        /// <summary>
        /// <see cref="Register(string,System.Type,string)"/>.
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="Behavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public StepRegistrationSequence Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            Guard.AgainstNull(nameof(wellKnownStep), wellKnownStep);

            return Register((string)wellKnownStep, behavior, description);
        }


        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        public void Register<TRegisterStep>() where TRegisterStep : RegisterStep, new()
        {
            AddStep(new TRegisterStep());
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="registration">The step registration.</param>
        public void Register(RegisterStep registration)
        {
            Guard.AgainstNull(nameof(registration), registration);
            AddStep(registration);
        }

        void AddStep(RegisterStep step)
        {
            registeredSteps.Add(step);

            modifications.Additions.Add(step);
        }

        List<RegisterStep> registeredSteps = new List<RegisterStep>();
        List<Type> registeredBehaviors = new List<Type>();

        PipelineModifications modifications;


        internal void RegisterConnector<T>(string description) where T : IStageConnector
        {
            Register(typeof(T).Name, typeof(T), description);
        }
    }
}