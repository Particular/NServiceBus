namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Basic <see cref="IBehavior{TContext}"/> registration class.
    /// </summary>
    public abstract class RegisterBehavior
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterBehavior"/> class.
        /// </summary>
        /// <param name="stepId">The unique identifier for this steps.</param>
        /// <param name="behavior">The type of <see cref="IBehavior{TContext}"/> to register.</param>
        /// <param name="description">A brief description of what this step does.</param>
        protected RegisterBehavior(string stepId, Type behavior, string description)
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
        /// Instructs the pipeline to register this <see cref="IBehavior{TContext}"/> before the <paramref name="id"/> one. If the <paramref name="id"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="id">The unique identifier of a different <see cref="IBehavior{TContext}"/> that we want to insert before.</param>
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
        /// Instructs the pipeline to register this <see cref="IBehavior{TContext}"/> before the <paramref name="id"/> one.
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
        /// Instructs the pipeline to register this <see cref="IBehavior{TContext}"/> after the <paramref name="id"/> one. If the <paramref name="id"/> does not exist, this condition is ignored. 
        /// </summary>
        /// <param name="id">The unique identifier of a different <see cref="IBehavior{TContext}"/> that we want to insert after.</param>
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
        /// Instructs the pipeline to register this <see cref="IBehavior{TContext}"/> after the <paramref name="id"/> one.
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

        internal static RegisterBehavior Create(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            return new DefaultRegisterBehavior(behavior, wellKnownStep, description);
        }
        internal static RegisterBehavior Create(string pipelineStep, Type behavior, string description)
        {
            return new DefaultRegisterBehavior(behavior, pipelineStep, description);
        }
        
        class DefaultRegisterBehavior : RegisterBehavior
        {
            public DefaultRegisterBehavior(Type behavior, string stepId, string description)
                : base(stepId, behavior, description)
            {
            }
        }
    }
}