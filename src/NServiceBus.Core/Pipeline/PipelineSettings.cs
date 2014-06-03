namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Settings;

    public class PipelineSettings
    {
        Configure config;

        public void Remove(string idToRemove)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            if (string.IsNullOrEmpty(idToRemove))
            {
                throw new ArgumentNullException("idToRemove");
            }

            var removals = config.Settings.Get<List<RemoveBehavior>>("Pipeline.Removals");

            removals.Add(new RemoveBehavior(idToRemove));
        }

        public void Replace(string idToReplace, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");

            if (string.IsNullOrEmpty(idToReplace))
            {
                throw new ArgumentNullException("idToReplace");
            }

            var replacements = config.Settings.Get<List<ReplaceBehavior>>("Pipeline.Replacements");

            replacements.Add(new ReplaceBehavior(idToReplace, newBehavior, description));
        }

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

            var additions = config.Settings.Get<List<RegisterBehavior>>("Pipeline.Additions");

            additions.Add(RegisterBehavior.Create(id, behavior, description));
        }


        public void Register<T>() where T : RegisterBehavior, new()
        {
            var additions = SettingsHolder.Instance.Get<List<RegisterBehavior>>("Pipeline.Additions");

            additions.Add(new T());
        }


        public PipelineSettings(Configure config)
        {
            this.config = config;
        
        }
    }
}