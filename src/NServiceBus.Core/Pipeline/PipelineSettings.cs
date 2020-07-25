namespace NServiceBus.Pipeline
{
    using System;
    using Configuration.AdvancedExtensibility;
    using ObjectBuilder;
    using Settings;

    /// <summary>
    /// Manages the pipeline configuration.
    /// </summary>
    public class PipelineSettings : ExposeSettings
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PipelineSettings" />.
        /// </summary>
        internal PipelineSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the step to remove.</param>
        public void Remove(string stepId)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            EnsureWriteEnabled(stepId, "remove");

            modifications.Removals.Add(new RemoveStep(stepId));
        }

        /// <summary>
        /// Replaces an existing step behavior with a new one.
        /// </summary>
        /// <param name="stepId">The identifier of the step to replace its implementation.</param>
        /// <param name="newBehavior">The new <see cref="Behavior{TContext}" /> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(string stepId, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, nameof(newBehavior));
            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            EnsureWriteEnabled(stepId, "replace");

            modifications.Replacements.Add(new ReplaceStep(stepId, newBehavior, description));
        }

        /// <summary>
        /// Replaces an existing step behavior with a new one.
        /// </summary>
        /// <param name="stepId">The identifier of the step to replace its implementation.</param>
        /// <param name="newBehavior">The new <see cref="Behavior{TContext}" /> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace<T>(string stepId, T newBehavior, string description = null)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(newBehavior));
            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            EnsureWriteEnabled(stepId, "replace");

            modifications.Replacements.Add(new ReplaceStep(stepId, typeof(T), description, builder => newBehavior));
        }

        /// <summary>
        /// Replaces an existing step behavior with a new one.
        /// </summary>
        /// <param name="stepId">The identifier of the step to replace its implementation.</param>
        /// <param name="factoryMethod">The factory method to create new instances of the behavior.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace<T>(string stepId, Func<IBuilder, T> factoryMethod, string description = null)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "newBehavior");
            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            EnsureWriteEnabled(stepId, "replace");

            modifications.Replacements.Add(new ReplaceStep(stepId, typeof(T), description, b => factoryMethod(b)));
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="behavior">The <see cref="Behavior{TContext}" /> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register(Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, nameof(behavior));

            Register(behavior.Name, behavior, description);
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="Behavior{TContext}" /> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, nameof(behavior));
            EnsureWriteEnabled(stepId, "register");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            modifications.Additions.Add(RegisterStep.Create(stepId, behavior, description));
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="factoryMethod">A callback that creates the behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register<T>(Func<IBuilder, T> factoryMethod, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");

            Register(typeof(T).Name, factoryMethod, description);
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="factoryMethod">A callback that creates the behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register<T>(string stepId, Func<IBuilder, T> factoryMethod, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), "behavior");
            EnsureWriteEnabled(stepId, "register");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            modifications.Additions.Add(RegisterStep.Create(stepId, typeof(T), description, b => factoryMethod(b)));
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="behavior">The behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register<T>(T behavior, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(behavior));

            Register(typeof(T).Name, behavior, description);
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="stepId">The identifier of the new step to add.</param>
        /// <param name="behavior">The behavior instance.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register<T>(string stepId, T behavior, string description)
            where T : IBehavior
        {
            BehaviorTypeChecker.ThrowIfInvalid(typeof(T), nameof(behavior));
            EnsureWriteEnabled(nameof(stepId), "register");

            Guard.AgainstNullAndEmpty(nameof(stepId), stepId);
            Guard.AgainstNullAndEmpty(nameof(description), description);

            modifications.Additions.Add(RegisterStep.Create(stepId, typeof(T), description, _ => behavior));
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        public void Register<TRegisterStep>() where TRegisterStep : RegisterStep, new()
        {
            EnsureWriteEnabled(nameof(TRegisterStep), "register");

            modifications.Additions.Add(new TRegisterStep());
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="registration">The step registration.</param>
        public void Register(RegisterStep registration)
        {
            Guard.AgainstNull(nameof(registration), registration);
            EnsureWriteEnabled(nameof(registration), "register");

            modifications.Additions.Add(registration);
        }

        /// <summary>
        /// Locks the pipeline settings to prevent further modifications.
        /// </summary>
        internal void PreventChanges()
        {
            locked = true;
        }

        void EnsureWriteEnabled(string key, string operation)
        {
            if (locked)
            {
                throw new InvalidOperationException($"Unable to {operation} the pipeline step for key: {key}. The pipeline has been locked for modifications. Move any configuration code before the endpoint is started.");
            }
        }

        bool locked;
        internal PipelineModifications modifications = new PipelineModifications();
    }
}