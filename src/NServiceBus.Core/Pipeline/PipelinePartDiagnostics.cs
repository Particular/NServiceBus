#nullable enable
namespace NServiceBus;

using System.Text;

static class PipelinePartDiagnostics
{
    public static string PrettyPrint(PipelinePart[] parts)
    {
        if (parts.Length == 0)
        {
            return "context => Task.CompletedTask";
        }

        var sb = new StringBuilder();
        var firstPart = parts[0];
        sb.Append($"({firstPart.ContextTypeName} context0) => {firstPart.BehaviorTypeName}.Invoke(context0,");

        if (HasChildren(firstPart))
        {
            AppendPrettyRecursive(parts, firstPart.ChildStart, firstPart.ChildEnd, sb, 1);
        }
        else
        {
            AppendPrettyRecursive(parts, 1, parts.Length, sb, 1);
        }

        sb.Append(')');
        return sb.ToString();
    }

    static void AppendPrettyRecursive(PipelinePart[] parts, int index, int rangeEnd, StringBuilder sb, int invokeCount)
    {
        if (index >= rangeEnd)
        {
            return;
        }

        var part = parts[index];
        var nextContextName = $"context{invokeCount}";

        if (HasChildren(part))
        {
            sb.AppendLine();
            sb.Append(new string(' ', invokeCount * 4));
            sb.Append($"({part.ContextTypeName} {nextContextName}) => {part.BehaviorTypeName}.Invoke({nextContextName}{(index + 1 < rangeEnd ? "," : string.Empty)}");
            AppendPrettyRecursive(parts, part.ChildStart, part.ChildEnd, sb, invokeCount + 1);
        }
        else
        {
            sb.AppendLine();
            sb.Append(new string(' ', invokeCount * 4));
            sb.Append($"({part.ContextTypeName} {nextContextName}) => {part.BehaviorTypeName}.Invoke({nextContextName}{(index + 1 < rangeEnd ? "," : string.Empty)}");
            AppendPrettyRecursive(parts, index + 1, rangeEnd, sb, invokeCount + 1);
        }

        sb.Append(')');
    }

    static bool HasChildren(in PipelinePart part) => part.ChildStart < part.ChildEnd;
}