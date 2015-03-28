namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Base class to do an advance registration of a step.
    /// </summary>
    [DebuggerDisplay("{StepId}({BehaviorType.FullName}) - {Description}")]
    public abstract class RegisterStep
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterStep"/> class.
        /// </summary>
        /// <param name="stepId">The unique identifier for this steps.</param>
        /// <param name="behavior">The type of <see cref="Behavior{TContext}"/> to register.</param>
        /// <param name="description">A brief description of what this step does.</param>
        /// <param name="isStatic">If a behavior is pipeline-static (shared between executions)</param>
        protected RegisterStep(string stepId, Type behavior, string description, bool isStatic = false)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");
            Guard.AgainstNullAndEmpty(stepId, "stepId");
            Guard.AgainstNullAndEmpty(description, "description");

            BehaviorType = behavior;
            StepId = stepId;
            Description = description;
            IsStatic = isStatic;
        }


        /// <summary>
        /// Allows for customization of the container registration for this step
        /// </summary>
        /// <param name="customRegistration"></param>
        public void ContainerRegistration<T>(Func<IBuilder,ReadOnlySettings,T> customRegistration)
        {
            Guard.AgainstNull(customRegistration, "customRegistration");
            customContainerRegistration = (settings, container) => container.ConfigureComponent(builder => customRegistration(builder,settings), DependencyLifecycle.InstancePerCall);
        }

        internal void ApplyContainerRegistration(ReadOnlySettings settings, IConfigureComponents container)
        {
            if (customContainerRegistration != null)
            {
                customContainerRegistration(settings, container);
                return;
            }

            container.ConfigureComponent(BehaviorType, DependencyLifecycle.InstancePerCall);
        }

        Action<ReadOnlySettings,IConfigureComponents> customContainerRegistration;

        /// <summary>
        /// Gets the unique identifier for this step.
        /// </summary>
        public string StepId { get; private set; }

        /// <summary>
        /// Gets if the behavior is pipeline-static (shared between all executions of the pipeline)
        /// </summary>
        public bool IsStatic { get; private set; }
        
        /// <summary>
        /// Gets the description for this registration.
        /// </summary>
        public string Description { get; internal set; }

        internal IList<Dependency> Befores { get; private set; }
        internal IList<Dependency> Afters { get; private set; }
        
        /// <summary>
        /// Gets the type of <see cref="Behavior{TContext}"/> that is being registered.
        /// </summary>
        public Type BehaviorType { get; internal set; }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="step"/> one. If the <paramref name="step"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="step">The <see cref="WellKnownStep"/> that we want to insert before.</param>
        public void InsertBeforeIfExists(WellKnownStep step)
        {
            Guard.AgainstNull(step, "step");

            InsertBeforeIfExists((string) step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="id"/> one. If the <paramref name="id"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="id">The unique identifier of the step that we want to insert before.</param>
        public void InsertBeforeIfExists(string id)
        {
            Guard.AgainstNullAndEmpty(id, "id");

            if (Befores == null)
            {
                Befores = new List<Dependency>();
            }

            Befores.Add(new Dependency(StepId,id,Dependency.DependencyDirection.Before, false));
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="step"/> one.
        /// </summary>
        public void InsertBefore(WellKnownStep step)
        {
            Guard.AgainstNull(step, "step");

            InsertBefore((string)step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="id"/> one.
        /// </summary>
        public void InsertBefore(string id)
        {
            Guard.AgainstNullAndEmpty(id, "id");

            if (Befores == null)
            {
                Befores = new List<Dependency>();
            }

            Befores.Add(new Dependency(StepId, id,Dependency.DependencyDirection.Before, true));
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="step"/> one. If the <paramref name="step"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="step">The unique identifier of the step that we want to insert after.</param>
        public void InsertAfterIfExists(WellKnownStep step)
        {
            Guard.AgainstNull(step, "step");

            InsertAfterIfExists((string)step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="id"/> one. If the <paramref name="id"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="id">The unique identifier of the step that we want to insert after.</param>
        public void InsertAfterIfExists(string id)
        {
            Guard.AgainstNullAndEmpty(id, "id");

            if (Afters == null)
            {
                Afters = new List<Dependency>();
            }

            Afters.Add(new Dependency(StepId, id, Dependency.DependencyDirection.After, false));
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="step"/> one.
        /// </summary>
        public void InsertAfter(WellKnownStep step)
        {
            Guard.AgainstNull(step, "step");

            InsertAfter((string)step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="id"/> one.
        /// </summary>
        public void InsertAfter(string id)
        {
            Guard.AgainstNullAndEmpty(id, "id");

            if (Afters == null)
            {
                Afters = new List<Dependency>();
            }

            Afters.Add(new Dependency(StepId, id, Dependency.DependencyDirection.After, true));
        }

        internal BehaviorInstance CreateBehavior(IBuilder defaultBuilder)
        {
            if (IsStatic)
            {
                return new StaticBehavior(BehaviorType, defaultBuilder);
            }
            return new PerCallBehavior(BehaviorType, defaultBuilder);
        }

        internal static RegisterStep Create(WellKnownStep wellKnownStep, Type behavior, string description, bool isStatic)
        {
            return new DefaultRegisterStep(behavior, wellKnownStep, description, isStatic);
        }
        internal static RegisterStep Create(string pipelineStep, Type behavior, string description, bool isStatic)
        {
            return new DefaultRegisterStep(behavior, pipelineStep, description, isStatic);
        }
        
        class DefaultRegisterStep : RegisterStep
        {
            public DefaultRegisterStep(Type behavior, string stepId, string description, bool isStatic)
                : base(stepId, behavior, description, isStatic)
            {
            }
        }
    }
}