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
        /// <param name="idToReplace">The identifier of the step to replace.</param>
        /// <param name="newBehavior">The new <see cref="IBehavior{TContext}"/> to use.</param>
        /// <param name="description">The description of the new behavior.</param>
        public void Replace(string idToReplace, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");

            if (string.IsNullOrEmpty(idToReplace))
            {
                throw new ArgumentNullException("idToReplace");
            }

            config.Settings.Get<PipelineModifications>().Replacements.Add(new ReplaceBehavior(idToReplace, newBehavior, description));
        }

        /// <summary>
        /// Register a new step into the pipeline.
        /// </summary>
        /// <param name="id">The identifier of the new step to add.</param>
        /// <param name="behavior">The <see cref="IBehavior{TContext}"/> to execute.</param>
        /// <param name="description">The description of the behavior.</param>
        public void Register(string id, Type behavior, string description)
        {
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            config.Settings.Get<PipelineModifications>().Additions.Add(RegisterBehavior.Create(id, behavior, description));
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