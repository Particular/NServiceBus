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
        internal PipelineSettings(PipelineModificationsBuilder modifications)
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
            Guard.AgainstNullAndEmpty("stepId", stepId);

            modifications.AddRemoval(new RemoveStep(stepId));
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to remove.</param>
        public void Remove(WellKnownStep wellKnownStep)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            Guard.AgainstNull("wellKnownStep", wellKnownStep);

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
            Guard.AgainstNullAndEmpty("stepId", stepId);

            registeredBehaviors.Add(newBehavior);
            modifications.AddReplacement(new ReplaceBehavior(stepId, newBehavior, description));
        }

        /// <summary>
        /// <see cref="Replace(string,System.Type,string)"/>.
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to replace.</param>
        /// <param name="newBehavior">The new <see cref="Behavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(WellKnownStep wellKnownStep, Type newBehavior, string description = null)
        {
            Guard.AgainstNull("wellKnownStep", wellKnownStep);

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

            Guard.AgainstNullAndEmpty("stepId", stepId);
            Guard.AgainstNullAndEmpty("description", description);

            AddStep(RegisterStep.Create(stepId, behavior, description));
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
            Guard.AgainstNull("wellKnownStep", wellKnownStep);

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
        /// <param name="customInitializer">A function the returns a new instance of the behavior.</param>
        public void Register<TRegisterStep, TBehavior>(Func<IBuilder, TBehavior> customInitializer) where TRegisterStep : RegisterStep, new()
        {
            Guard.AgainstNull("customInitializer", customInitializer);
            var registration = new TRegisterStep();

            registration.ContainerRegistration((b, s) => customInitializer(b));

            AddStep(registration);
        }


        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="registration">The step registration.</param>
        public void Register(RegisterStep registration)
        {
            Guard.AgainstNull("registration", registration);
            AddStep(registration);
        }

        void AddStep(RegisterStep step)
        {
            registeredSteps.Add(step);

            modifications.AddAddition(step);
        }

        List<RegisterStep> registeredSteps = new List<RegisterStep>();
        List<Type> registeredBehaviors = new List<Type>();

        PipelineModificationsBuilder modifications;


        internal void RegisterConnector<T>(string description) where T : IStageConnector
        {
            Register(typeof(T).Name, typeof(T), description);
        }
    }
}