﻿namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    class PipelineModifications
    {
        public List<RegisterStep> Additions = new List<RegisterStep>();
        public List<RemoveStep> Removals = new List<RemoveStep>();
        public List<ReplaceBehavior> Replacements = new List<ReplaceBehavior>();
    }
}