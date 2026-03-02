#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;
using System.Text;

static class PipelineStepDiagnostics
{
    public static string PrettyPrint(IReadOnlyList<RegisterStep> steps)
    {
        if (steps.Count == 0)
        {
            return "context => Task.CompletedTask";
        }

        var sb = new StringBuilder();
        var rootStep = steps[0];
        sb.Append($"({rootStep.InputContextType.Name} context0) => {rootStep.BehaviorType.Name}.Invoke(context0,");

        for (var i = 1; i < steps.Count; i++)
        {
            var step = steps[i];
            var nextContextName = $"context{i}";

            sb.AppendLine();
            sb.Append(new string(' ', i * 4));
            sb.Append($"({step.InputContextType.Name} {nextContextName}) => {step.BehaviorType.Name}.Invoke({nextContextName}{(i + 1 < steps.Count ? "," : string.Empty)}");
        }

        for (var i = 0; i < steps.Count; i++)
        {
            sb.Append(')');
        }

        return sb.ToString();
    }
}
