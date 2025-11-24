namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;

class PipelineModifications
{
    public List<RegisterStep> Additions = [];
    public List<ReplaceStep> Replacements = [];
    public List<RegisterOrReplaceStep> AdditionsOrReplacements = [];
}