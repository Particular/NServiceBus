namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    class PipelineModificationsBuilder
    {
        readonly List<RegisterStep> additions = new List<RegisterStep>();
        readonly List<RemoveStep> removals = new List<RemoveStep>();
        readonly List<ReplaceBehavior> replacements = new List<ReplaceBehavior>();

        public void AddRemoval(RemoveStep removeStep)
        {
            removals.Add(removeStep);
        }

        public void AddReplacement(ReplaceBehavior replaceBehavior)
        {
            replacements.Add(replaceBehavior);
        }

        public void AddAddition(RegisterStep step)
        {
            additions.Add(step);
        }

        public IEnumerable<RegisterStep> Additions { get { return additions; }}
        public IEnumerable<RemoveStep> Removals { get { return removals; }}
        public IEnumerable<ReplaceBehavior> Replacements { get { return replacements; }}
    }
}