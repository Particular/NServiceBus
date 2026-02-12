#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;

class StepRegistrationsCoordinator(
    IReadOnlyCollection<RegisterStep> additions,
    IReadOnlyCollection<ReplaceStep> replacements,
    IReadOnlyCollection<RegisterOrReplaceStep> addOrReplaceSteps)
{
    public PipelineBuildModel BuildPipelineBuildModelFor<TRootContext>() where TRootContext : IBehaviorContext
    {
        var pipelineModelBuilder = new PipelineModelBuilder(typeof(TRootContext), additions, replacements, addOrReplaceSteps);
        return pipelineModelBuilder.BuildModel();
    }
}