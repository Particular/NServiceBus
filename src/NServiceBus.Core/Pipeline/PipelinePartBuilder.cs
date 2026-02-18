#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Pipeline;

static class PipelinePartBuilder
{
    public static PipelinePart[] BuildParts(PipelineModel model)
    {
        if (model.Steps.Count == 0)
        {
            return [];
        }

        var parts = new List<PipelinePart>(model.Steps.Count);
        BuildStage(model.Stages, 0, parts);

        return [.. parts];
    }

    static void BuildStage(IReadOnlyList<PipelineStageModel> stages, int stageIndex, List<PipelinePart> parts)
    {
        if (stageIndex < 0 || stageIndex >= stages.Count)
        {
            return;
        }

        var stage = stages[stageIndex];

        foreach (var behavior in stage.Behaviors)
        {
            parts.Add(CreateBehaviorPart(behavior));
        }

        var connector = stage.Connector;
        if (connector == null)
        {
            return;
        }

        var connectorPartIndex = parts.Count;
        parts.Add(default);

        var childStart = parts.Count;

        if (stage.NextContextType != null)
        {
            if (stageIndex + 1 < stages.Count)
            {
                if (stages[stageIndex + 1].ContextType != stage.NextContextType)
                {
                    throw new InvalidOperationException($"Stage metadata inconsistency for connector '{connector.BehaviorType.FullName}'. Expected next context '{stage.NextContextType.FullName}'.");
                }

                BuildStage(stages, stageIndex + 1, parts);
            }
        }

        var childEnd = parts.Count;

        parts[connectorPartIndex] = CreateStagePart(stage.IsTerminator, connector, childStart, childEnd);
    }

    static PipelinePart CreateBehaviorPart(RegisterStep step)
    {
        var behaviorType = step.BehaviorType;
        var contextType = step.InputContextType;

        var invokerId = PipelineInvokers.GetBehaviorId(contextType);
        return new PipelinePart(invokerId, 0, 0, behaviorType.Name, contextType.Name);
    }

    static PipelinePart CreateStagePart(bool isTerminator, RegisterStep step, int childStart, int childEnd)
    {
        var behaviorType = step.BehaviorType;
        var inContextType = step.InputContextType;
        var outContextType = step.OutputContextType;

        var invokerId = isTerminator ? PipelineInvokers.StageToTerminating : PipelineInvokers.GetStageId(inContextType, outContextType);
        return new PipelinePart(invokerId, childStart, childEnd, behaviorType.Name, inContextType.Name);
    }
}