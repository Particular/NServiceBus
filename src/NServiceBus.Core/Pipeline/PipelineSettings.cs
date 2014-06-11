namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

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

            List<RemoveBehavior> removals;

            if (!config.Settings.TryGet("Pipeline.Removals",out removals))
            {
                removals = new List<RemoveBehavior>();
            
                config.Settings.Set("Pipeline.Removals", removals);
            }
            
            removals.Add(new RemoveBehavior(idToRemove));
        }

        public void Replace(string idToReplace, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");

            if (string.IsNullOrEmpty(idToReplace))
            {
                throw new ArgumentNullException("idToReplace");
            }


            List<ReplaceBehavior> replacements;

            if (!config.Settings.TryGet("Pipeline.Replacements", out replacements))
            {
                replacements = new List<ReplaceBehavior>();

                config.Settings.Set("Pipeline.Replacements", replacements);
            }

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

            List<RegisterBehavior> additions;

            if (!config.Settings.TryGet("Pipeline.Additions", out additions))
            {
                additions = new List<RegisterBehavior>();

                config.Settings.Set("Pipeline.Additions", additions);
            }

            additions.Add(RegisterBehavior.Create(id, behavior, description));
        }


        public void Register<T>() where T : RegisterBehavior, new()
        {
            List<RegisterBehavior> additions;

            if (!config.Settings.TryGet("Pipeline.Additions", out additions))
            {
                additions = new List<RegisterBehavior>();

                config.Settings.Set("Pipeline.Additions", additions);
            }

            additions.Add(new T());
        }


        public PipelineSettings(Configure config)
        {
            this.config = config;
        
        }
    }
}