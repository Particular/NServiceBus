namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<ReplaceStep> Replacements = new List<ReplaceStep>();
        public List<RegisterOrReplaceStep> AdditionsOrReplacements = new List<RegisterOrReplaceStep>();
    }
}