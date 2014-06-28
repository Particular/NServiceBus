namespace NServiceBus.Pipeline
{
    using System;

    /// <summary>
    /// Manages the pipeline configuration.
    /// </summary>
    public class PipelineSettings
    {
        internal PipelineSettings(Configure config)
        {
            this.config = config;
        }

        /// <summary>
        /// Removes the specified step from the pipeline.
        /// </summary>
        /// <param name="idToRemove">The identifier of the step to remove.</param>
        public void Remove(string idToRemove)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            if (string.IsNullOrEmpty(idToRemove))
            {
                throw new ArgumentNullException("idToRemove");
            }


            config.Settings.Get<PipelineModifications>().Removals.Add(new RemoveBehavior(idToRemove));
        }

        /// <summary>
        /// Replaces an existing step behavior with a new one.
        /// </summary>
        /// <param name="pipelineStep">The identifier of the step to replace.</param>
        /// <param name="newBehavior">The new <see cref="IBehavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(string pipelineStep, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");

            if (string.IsNullOrEmpty(pipelineStep))
            {
                throw new ArgumentNullException("pipelineStep");
            }

            config.Settings.Get<PipelineModifications>().Replacements.Add(new ReplaceBehavior(pipelineStep, newBehavior, description));
        }

        /// <summary>
        /// <see cref="Replace(string,System.Type,string)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the well known step to replace.</param>
        /// <param name="newBehavior">The new <see cref="IBehavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(WellKnownStep wellKnownStep, Type newBehavior, string description = null)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            Replace((string)wellKnownStep, newBehavior, description);
        }
        
        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="pipelineStep">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register(string pipelineStep, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            if (string.IsNullOrEmpty(pipelineStep))
            {
                throw new ArgumentNullException("pipelineStep");
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            config.Settings.Get<PipelineModifications>().Additions.Add(RegisterBehavior.Create(pipelineStep, behavior, description));
        }


        /// <summary>
        /// <see cref="Register(string,System.Type,string)"/>
        /// </summary>
        /// <param name="wellKnownStep">The identifier of the step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            if (wellKnownStep == null)
            {
                throw new ArgumentNullException("wellKnownStep");
            }

            Register((string)wellKnownStep, behavior, description);
        }


        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <typeparam name="T">The <see cref="RegisterBehavior"/> to use to perform the registration.</typeparam>
        public void Register<T>() where T : RegisterBehavior, new()
        {
            config.Settings.Get<PipelineModifications>().Additions.Add(new T());
        }

        Configure config;
    }
}