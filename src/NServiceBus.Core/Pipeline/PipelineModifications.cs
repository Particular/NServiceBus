namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<RemoveBehavior> Removals = new List<RemoveBehavior>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }
}