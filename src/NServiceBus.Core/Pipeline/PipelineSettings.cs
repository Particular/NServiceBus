namespace NServiceBus.Pipeline
{
    using System;

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


            config.Settings.Get<PipelineModifications>().Removals.Add(new RemoveBehavior(idToRemove));
        }

        public void Replace(string idToReplace, Type newBehavior, string description = null)
        {
            BehaviorTypeChecker.ThrowIfInvalid(newBehavior, "newBehavior");

            if (string.IsNullOrEmpty(idToReplace))
            {
                throw new ArgumentNullException("idToReplace");
            }

            config.Settings.Get<PipelineModifications>().Replacements.Add(new ReplaceBehavior(idToReplace, newBehavior, description));
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

            config.Settings.Get<PipelineModifications>().Additions.Add(RegisterBehavior.Create(id, behavior, description));
        }


        public void Register<T>() where T : RegisterBehavior, new()
        {
            config.Settings.Get<PipelineModifications>().Additions.Add(new T());
        }


        public PipelineSettings(Configure config)
        {
            this.config = config;
        
        }
    }
}