namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class to do an advance registration of a step.
    /// </summary>
    public abstract class RegisterStep
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterStep"/> class.
        /// </summary>
        /// <param name="stepId">The unique identifier for this steps.</param>
        /// <param name="behavior">The type of <see cref="IBehavior{TContext}"/> to register.</param>
        /// <param name="description">A brief description of what this step does.</param>
        protected RegisterStep(string stepId, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            if (String.IsNullOrEmpty(stepId))
            {
                throw new ArgumentNullException("stepId");
            }

            if (String.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            BehaviorType = behavior;
            StepId = stepId;
            Description = description;
        }

        /// <summary>
        /// Gets the unique identifier for this step.
        /// </summary>
        public string StepId { get; private set; }
        
        /// <summary>
        /// Gets the description for this registration.
        /// </summary>
        public string Description { get; internal set; }

        internal IList<Dependency> Befores { get; private set; }
        internal IList<Dependency> Afters { get; private set; }
        
        /// <summary>
        /// Gets the type of <see cref="IBehavior{TContext}"/> that is being registered.
        /// </summary>
        public Type BehaviorType { get; internal set; }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="step"/> one. If the <paramref name="step"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="step">The <see cref="WellKnownStep"/> that we want to insert before.</param>
        public void InsertBeforeIfExists(WellKnownStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            InsertBeforeIfExists((string) step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="id"/> one. If the <paramref name="id"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="id">The unique identifier of the step that we want to insert before.</param>
        public void InsertBeforeIfExists(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Befores == null)
            {
                Befores = new List<Dependency>();
            }

            Befores.Add(new Dependency(id, false));
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="step"/> one.
        /// </summary>
        public void InsertBefore(WellKnownStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            InsertBefore((string)step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="id"/> one.
        /// </summary>
        public void InsertBefore(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Befores == null)
            {
                Befores = new List<Dependency>();
            }

            Befores.Add(new Dependency(id, true));
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="step"/> one. If the <paramref name="step"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="step">The unique identifier of the step that we want to insert after.</param>
        public void InsertAfterIfExists(WellKnownStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            InsertAfterIfExists((string)step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="id"/> one. If the <paramref name="id"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="id">The unique identifier of the step that we want to insert after.</param>
        public void InsertAfterIfExists(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Afters == null)
            {
                Afters = new List<Dependency>();
            }

            Afters.Add(new Dependency(id, false));
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="step"/> one.
        /// </summary>
        public void InsertAfter(WellKnownStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            InsertAfter((string)step);
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="id"/> one.
        /// </summary>
        public void InsertAfter(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (Afters == null)
            {
                Afters = new List<Dependency>();
            }

            Afters.Add(new Dependency(id, true));
        }

        internal static RegisterStep Create(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            return new DefaultRegisterStep(behavior, wellKnownStep, description);
        }
        internal static RegisterStep Create(string pipelineStep, Type behavior, string description)
        {
            return new DefaultRegisterStep(behavior, pipelineStep, description);
        }
        
        class DefaultRegisterStep : RegisterStep
        {
            public DefaultRegisterStep(Type behavior, string stepId, string description)
                : base(stepId, behavior, description)
            {
            }
        }
    }
}