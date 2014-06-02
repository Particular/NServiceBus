namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Settings;

    public class PipelineSettings
    {
        readonly Configure config;

        public void Remove(string idToRemove)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            if (String.IsNullOrEmpty(idToRemove))
            {
                throw new ArgumentNullException("idToRemove");
            }

            var removals = config.Settings.Get<List<RemoveBehavior>>("Pipeline.Removals");

            removals.Add(new RemoveBehavior(idToRemove));
        }

        public void Replace(string idToReplace, Type newBehavior, string description = null)
        {
            if (newBehavior.IsAssignableFrom(iBehaviourType))
            {
                throw new ArgumentException("TBehavior needs to implement IBehavior<TContext>");
            }

            if (String.IsNullOrEmpty(idToReplace))
            {
                throw new ArgumentNullException("idToReplace");
            }

            var replacements = config.Settings.Get<List<ReplaceBehavior>>("Pipeline.Replacements");

            replacements.Add(new ReplaceBehavior(idToReplace, newBehavior, description));
        }

        public void Register(string id, Type behavior, string description)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException("id");
            }

            if (behavior.IsAssignableFrom(iBehaviourType))
            {
                throw new ArgumentException("Needs to implement IBehavior<TContext>", "behavior");
            }

            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (String.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            var additions = config.Settings.Get<List<RegisterBehavior>>("Pipeline.Additions");

            additions.Add(RegisterBehavior.Create(id, behavior, description));
        }

        public void Register<T>() where T : RegisterBehavior, new()
        {
            var additions = SettingsHolder.Instance.Get<List<RegisterBehavior>>("Pipeline.Additions");

            additions.Add(new T());
        }

        static Type iBehaviourType = typeof(IBehavior<>);

        public PipelineSettings(Configure config)
        {
            this.config = config;
        
        }
    }
}