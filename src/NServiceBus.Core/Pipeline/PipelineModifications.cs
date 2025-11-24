namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;

class PipelineModifications
{
    public readonly List<RegisterStep> Additions = [];
    public readonly List<ReplaceStep> Replacements = [];
    public readonly List<RegisterOrReplaceStep> AdditionsOrReplacements = [];
}