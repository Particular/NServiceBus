#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Logging;
using Pipeline;

class PipelineModelBuilder(
    Type rootContextType,
    IReadOnlyCollection<RegisterStep> additions,
    IReadOnlyCollection<ReplaceStep> replacements,
    IReadOnlyCollection<RegisterOrReplaceStep> addOrReplaceSteps)
{
    public IReadOnlyCollection<RegisterStep> Build() => BuildModel().Steps;

    public PipelineBuildModel BuildModel()
    {
        var registrations = new Dictionary<string, RegisterStep>(StringComparer.CurrentCultureIgnoreCase);

        var additionsFromRegisterOrReplace = new Dictionary<string, RegisterStep>(StringComparer.CurrentCultureIgnoreCase);
        var replacementsFromRegisterOrReplace = new Dictionary<string, ReplaceStep>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var addOrReplaceStep in addOrReplaceSteps)
        {
            var stepId = addOrReplaceStep.StepId;
            var hasExistingAddition = additions.Any(addition => addition.StepId == stepId);

            if (hasExistingAddition)
            {
                replacementsFromRegisterOrReplace[stepId] = addOrReplaceStep.ReplaceStep;
            }
            else
            {
                additionsFromRegisterOrReplace[stepId] = addOrReplaceStep.RegisterStep;
            }
        }

        var totalAdditions = new List<RegisterStep>(additionsFromRegisterOrReplace.Count + additions.Count);
        totalAdditions.AddRange(additionsFromRegisterOrReplace.Values);
        totalAdditions.AddRange(additions);

        var totalReplacements = new List<ReplaceStep>(replacementsFromRegisterOrReplace.Count + replacements.Count);
        totalReplacements.AddRange(replacementsFromRegisterOrReplace.Values);
        totalReplacements.AddRange(replacements);
        totalReplacements.Sort(static (x, y) => x.RegistrationOrder.CompareTo(y.RegistrationOrder));

        //Step 1: validate that additions are unique
        foreach (var metadata in totalAdditions)
        {
            if (!registrations.TryGetValue(metadata.StepId, out var existingValue))
            {
                registrations.Add(metadata.StepId, metadata);

                continue;
            }

            var message = $"Step registration with id '{metadata.StepId}' is already registered for '{existingValue.BehaviorType}'.";
            throw new Exception(message);
        }

        //  Step 2: validate and apply replacements
        foreach (var metadata in totalReplacements)
        {
            if (!registrations.TryGetValue(metadata.ReplaceId, out var value))
            {
                var message = $"'{metadata.ReplaceId}' cannot be replaced because it does not exist. Make sure that you only register a replacement for existing pipeline behaviors.";
                throw new Exception(message);
            }

            var registerStep = value;
            registerStep.Replace(metadata);
        }

        var stages = new Dictionary<Type, List<RegisterStep>>();
        foreach (var registration in registrations.Values)
        {
            var inputContext = registration.InputContextType;
            if (!stages.TryGetValue(inputContext, out var list))
            {
                list = [];
                stages[inputContext] = list;
            }
            list.Add(registration);
        }

        var finalOrder = new List<RegisterStep>(registrations.Count);
        var orderedStages = new List<PipelineStageModel>(stages.Count);

        if (registrations.Count == 0)
        {
            return new PipelineBuildModel(rootContextType, finalOrder, orderedStages);
        }

        if (!stages.TryGetValue(rootContextType, out var currentStage))
        {
            throw new Exception($"Can't find any behaviors/connectors for the root context ({rootContextType.FullName})");
        }

        var currentStageContextType = rootContextType;
        var stageNumber = 1;
        var totalStages = stages.Count;

        while (currentStage != null)
        {
            var stageContextType = currentStageContextType;
            var stageSteps = new List<RegisterStep>(currentStage.Count);
            List<RegisterStep>? stageConnectors = null;

            foreach (var step in currentStage)
            {
                if (step.IsStageConnector)
                {
                    stageConnectors ??= [];
                    stageConnectors.Add(step);
                }
                else
                {
                    stageSteps.Add(step);
                }
            }

            var sortedStageSteps = Sort(stageSteps);
            finalOrder.AddRange(sortedStageSteps);

            if (stageConnectors is { Count: > 1 })
            {
                var connectors = $"'{string.Join("', '", stageConnectors.Select(sc => sc.BehaviorType.FullName))}'";
                throw new Exception($"Multiple stage connectors found for stage '{currentStageContextType.FullName}'. Remove one of: {connectors}");
            }

            var stageConnector = stageConnectors?[0];
            Type? nextContextType = null;
            var isTerminator = false;

            if (stageConnector == null)
            {
                if (stageNumber < totalStages)
                {
                    throw new Exception($"No stage connector found for stage '{currentStageContextType.FullName}'.");
                }

                currentStage = null;
            }
            else
            {
                finalOrder.Add(stageConnector);

                if (stageConnector.IsTerminator)
                {
                    isTerminator = true;
                    currentStage = null;
                }
                else
                {
                    var stageEndType = stageConnector.OutputContextType;
                    nextContextType = stageEndType;
                    currentStageContextType = stageEndType;
                    currentStage = stages.GetValueOrDefault(stageEndType);
                }
            }

            orderedStages.Add(new PipelineStageModel(
                stageContextType,
                sortedStageSteps,
                stageConnector,
                nextContextType,
                isTerminator));

            stageNumber++;
        }

        return new PipelineBuildModel(rootContextType, finalOrder, orderedStages);
    }

    static List<RegisterStep> Sort(List<RegisterStep> registrations)
    {
        if (registrations.Count == 0)
        {
            return registrations;
        }

        // Step 1: create nodes for graph
        var count = registrations.Count;
        var nameToNode = new Dictionary<string, Node>(count);
        var allNodes = new List<Node>(count);
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
        var output = new List<RegisterStep>(count);
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
            var message = $"Registration '{beforeReference.DependsOnId}' specified in the insertbefore of the '{node.StepId}' step does not exist. Current StepIds: {currentStepIds}";

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
            output.Add(registerStep);
        }

        public readonly List<Dependency>? Afters = registerStep.Afters;
        public readonly List<Dependency>? Befores = registerStep.Befores;

        public readonly string StepId = registerStep.StepId;
        internal readonly List<Node> previous = [];
        bool visited;
    }
}