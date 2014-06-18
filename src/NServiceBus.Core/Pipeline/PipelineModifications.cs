namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    class PipelineModifications
    {
        public List<RegisterBehavior> Additions = new List<RegisterBehavior>();
        public List<RemoveBehavior> Removals = new List<RemoveBehavior>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }
}