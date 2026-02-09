#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Pipeline;

static class PipelinePartBuilder
{
    public static PipelinePart[] BuildParts(PipelineBuildModel model)
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
        if ((uint)stageIndex >= (uint)stages.Count)
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

        parts[connectorPartIndex] = CreateStagePart(connector, childStart, childEnd);
    }

    static PipelinePart CreateBehaviorPart(RegisterStep step)
    {
        var behaviorType = step.BehaviorType;
        var interfaceType = GetBehaviorInterface(behaviorType);
        var genericArgs = interfaceType.GetGenericArguments();
        var contextType = genericArgs[0];

        var method = typeof(BehaviorPartFactory).GetMethod("Create")!
            .MakeGenericMethod(contextType, behaviorType);

        return (PipelinePart)method.Invoke(null, [])!;
    }

    static PipelinePart CreateStagePart(RegisterStep step, int childStart, int childEnd)
    {
        var behaviorType = step.BehaviorType;
        var interfaceType = GetBehaviorInterface(behaviorType);
        var genericArgs = interfaceType.GetGenericArguments();
        var inContextType = genericArgs[0];
        var outContextType = genericArgs[1];

        var method = typeof(StagePartFactory).GetMethod("Create")!
            .MakeGenericMethod(inContextType, outContextType, behaviorType);

        return (PipelinePart)method.Invoke(null, [childStart, childEnd])!;
    }

    static Type GetBehaviorInterface(Type behaviorType)
    {
        var interfaces = behaviorType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBehavior<,>));

        return interfaces.FirstOrDefault()
            ?? throw new InvalidOperationException($"Type '{behaviorType.FullName}' does not implement IBehavior<,>.");
    }
}