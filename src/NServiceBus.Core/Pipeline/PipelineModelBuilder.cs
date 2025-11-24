namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Logging;
using Pipeline;

class PipelineModelBuilder(Type rootContextType, List<RegisterStep> additions, List<ReplaceStep> replacements, List<RegisterOrReplaceStep> addOrReplaceSteps)
{
    public List<RegisterStep> Build()
    {
        var registrations = new Dictionary<string, RegisterStep>(StringComparer.CurrentCultureIgnoreCase);

        var totalAdditions = addOrReplaceSteps.Where(addOrReplaceStep => additions.All(addition => addition.StepId != addOrReplaceStep.StepId))
            .Select(x => x.RegisterStep)
            .ToList();
        var totalReplacements = addOrReplaceSteps.Where(addOrReplaceStep => additions.Any(addition => addition.StepId == addOrReplaceStep.StepId))
            .Select(x => x.ReplaceStep)
            .ToList();

        totalAdditions.AddRange(additions);
        totalReplacements.AddRange(replacements);

        //Step 1: validate that additions are unique
        foreach (var metadata in totalAdditions)
        {
            if (!registrations.TryGetValue(metadata.StepId, out RegisterStep existingValue))
            {
                registrations.Add(metadata.StepId, metadata);
                continue;
            }

            var message = $"Step registration with id '{metadata.StepId}' is already registered for '{existingValue.BehaviorType}'.";
            throw new Exception(message);
        }

        //  Step 2: validate and apply replacements
        var groupedReplacements = replacements.GroupBy(x => x.ReplaceId).ToList();
        if (groupedReplacements.Any(x => x.Count() > 1))
        {
            var duplicateReplaceIdentifiers = groupedReplacements.Where(x => x.Count() > 1).Select(x => $"'{x.Key}'");
            var duplicateIdentifiersList = string.Join(", ", duplicateReplaceIdentifiers);
            var message = $"Multiple replacements of the same pipeline behaviour is not supported. Make sure that you only register a single replacement for: {duplicateIdentifiersList}.";
            throw new Exception(message);
        }

        foreach (var metadata in totalReplacements)
        {
            if (!registrations.TryGetValue(metadata.ReplaceId, out RegisterStep value))
            {
                var message = $"Multiple replacements of the same pipeline behaviour is not supported. Make sure that you only register a single replacement for '{metadata.ReplaceId}'.";
                throw new Exception(message);
            }

            var registerStep = value;
            registerStep.Replace(metadata);
        }

        var stages = registrations.Values.GroupBy(r => r.GetInputContext()).ToList();

        var finalOrder = new List<RegisterStep>();

        if (registrations.Count == 0)
        {
            return finalOrder;
        }

        var currentStage = stages.SingleOrDefault(stage => stage.Key == rootContextType) ?? throw new Exception($"Can't find any behaviors/connectors for the root context ({rootContextType.FullName})");

        var stageNumber = 1;

        while (currentStage != null)
        {
            var stageSteps = currentStage.Where(stageStep => !IsStageConnector(stageStep)).ToList();

            //add the stage connector
            finalOrder.AddRange(Sort(stageSteps));

            var stageConnectors = currentStage.Where(IsStageConnector).ToList();

            if (stageConnectors.Count > 1)
            {
                var connectors = $"'{string.Join("', '", stageConnectors.Select(sc => sc.BehaviorType.FullName))}'";
                throw new Exception($"Multiple stage connectors found for stage '{currentStage.Key.FullName}'. Remove one of: {connectors}");
            }

            var stageConnector = stageConnectors.FirstOrDefault();

            if (stageConnector == null)
            {
                if (stageNumber < stages.Count)
                {
                    throw new Exception($"No stage connector found for stage {currentStage.Key.FullName}");
                }

                currentStage = null;
            }
            else
            {
                finalOrder.Add(stageConnector);

                if (typeof(IPipelineTerminator).IsAssignableFrom(stageConnector.BehaviorType))
                {
                    currentStage = null;
                }
                else
                {
                    var stageEndType = stageConnector.GetOutputContext();
                    currentStage = stages.SingleOrDefault(stage => stage.Key == stageEndType);
                }
            }

            stageNumber++;
        }

        return finalOrder;
    }

    static bool IsStageConnector(RegisterStep stageStep)
    {
        return typeof(IStageConnector).IsAssignableFrom(stageStep.BehaviorType);
    }

    static List<RegisterStep> Sort(List<RegisterStep> registrations)
    {
        if (registrations.Count == 0)
        {
            return registrations;
        }

        // Step 1: create nodes for graph
        var nameToNode = new Dictionary<string, Node>();
        var allNodes = new List<Node>();
        foreach (var rego in registrations)
        {
            // create entries to preserve order within
            var node = new Node(rego);
            nameToNode[rego.StepId] = node;
            allNodes.Add(node);
        }

        // Step 2: create edges from InsertBefore/InsertAfter values
        foreach (var node in allNodes)
        {
            ProcessBefores(node, nameToNode);
            ProcessAfters(node, nameToNode);
        }

        // Step 3: Perform Topological Sort
        var output = new List<RegisterStep>();
        foreach (var node in allNodes)
        {
            node.Visit(output);
        }

        return output;
    }

    static void ProcessBefores(Node node, Dictionary<string, Node> nameToNode)
    {
        if (node.Befores == null)
        {
            return;
        }

        foreach (var beforeReference in node.Befores)
        {
            if (nameToNode.TryGetValue(beforeReference.DependsOnId, out var referencedNode))
            {
                referencedNode.previous.Add(node);
                continue;
            }

            var currentStepIds = GetCurrentIds(nameToNode);
            var message = $"Registration '{beforeReference.DependsOnId}' specified in the InsertBefore of the '{node.StepId}' step does not exist. Current StepIds: {currentStepIds}";

            if (!beforeReference.Enforce)
            {
                Logger.Debug(message);
            }
            else
            {
                throw new Exception(message);
            }
        }
    }

    static void ProcessAfters(Node node, Dictionary<string, Node> nameToNode)
    {
        if (node.Afters == null)
        {
            return;
        }

        foreach (var afterReference in node.Afters)
        {
            if (nameToNode.TryGetValue(afterReference.DependsOnId, out var referencedNode))
            {
                node.previous.Add(referencedNode);
                continue;
            }

            var currentStepIds = GetCurrentIds(nameToNode);
            var message = $"Registration '{afterReference.DependsOnId}' specified in the insertafter of the '{node.StepId}' step does not exist. Current StepIds: {currentStepIds}";

            if (!afterReference.Enforce)
            {
                Logger.Debug(message);
            }
            else
            {
                throw new Exception(message);
            }
        }
    }

    static string GetCurrentIds(Dictionary<string, Node> nameToNodeDict) => $"'{string.Join("', '", nameToNodeDict.Keys)}'";

    static readonly ILog Logger = LogManager.GetLogger<PipelineModelBuilder>();

    class Node(RegisterStep registerStep)
    {
        internal void Visit(List<RegisterStep> output)
        {
            if (visited)
            {
                return;
            }

            visited = true;
            foreach (var n in previous)
            {
                n.Visit(output);
            }

            if (registerStep != null)
            {
                output.Add(registerStep);
            }
        }

        public readonly List<Dependency> Afters = registerStep.Afters;
        public readonly List<Dependency> Befores = registerStep.Befores;
        public readonly string StepId = registerStep.StepId;

        internal readonly List<Node> previous = [];
        bool visited;
    }
}