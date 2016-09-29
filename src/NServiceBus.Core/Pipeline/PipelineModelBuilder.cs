namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using Pipeline;

    class PipelineModelBuilder
    {
        public PipelineModelBuilder(Type rootContextType, List<RegisterStep> additions, List<RemoveStep> removals, List<ReplaceStep> replacements)
        {
            this.rootContextType = rootContextType;
            this.additions = additions;
            this.removals = removals;
            this.replacements = replacements;
        }

        public List<RegisterStep> Build()
        {
            var registrations = new Dictionary<string, RegisterStep>(StringComparer.CurrentCultureIgnoreCase);
            var listOfBeforeAndAfterIds = new List<string>();

            // Let's do some validation too

            //Step 1: validate that additions are unique
            foreach (var metadata in additions)
            {
                if (!registrations.ContainsKey(metadata.StepId))
                {
                    registrations.Add(metadata.StepId, metadata);
                    if (metadata.Afters != null)
                    {
                        listOfBeforeAndAfterIds.AddRange(metadata.Afters.Select(a => a.DependsOnId));
                    }
                    if (metadata.Befores != null)
                    {
                        listOfBeforeAndAfterIds.AddRange(metadata.Befores.Select(b => b.DependsOnId));
                    }

                    continue;
                }

                var message = $"Step registration with id '{metadata.StepId}' is already registered for '{registrations[metadata.StepId].BehaviorType}'.";
                throw new Exception(message);
            }

            //  Step 2: do replacements
            foreach (var metadata in replacements)
            {
                if (!registrations.ContainsKey(metadata.ReplaceId))
                {
                    var message = $"You can only replace an existing step registration, '{metadata.ReplaceId}' registration does not exist.";
                    throw new Exception(message);
                }

                var registerStep = registrations[metadata.ReplaceId];
                registerStep.Replace(metadata);
            }

            // Step 3: validate the removals
            foreach (var metadata in removals.Distinct(idComparer))
            {
                if (!registrations.ContainsKey(metadata.RemoveId))
                {
                    var message = $"You cannot remove step registration with id '{metadata.RemoveId}', registration does not exist.";
                    throw new Exception(message);
                }

                if (listOfBeforeAndAfterIds.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase))
                {
                    var add = additions.First(mr => (mr.Befores != null && mr.Befores.Select(b => b.DependsOnId).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)) ||
                                                    (mr.Afters != null && mr.Afters.Select(b => b.DependsOnId).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)));

                    var message = $"You cannot remove step registration with id '{metadata.RemoveId}', registration with id '{add.StepId}' depends on it.";
                    throw new Exception(message);
                }

                registrations.Remove(metadata.RemoveId);
            }

            var stages = registrations.Values.GroupBy(r => r.GetInputContext())
                .ToList();

            var finalOrder = new List<RegisterStep>();

            if (registrations.Count == 0)
            {
                return finalOrder;
            }

            var currentStage = stages.SingleOrDefault(stage => stage.Key == rootContextType);

            if (currentStage == null)
            {
                throw new Exception($"Can't find any behaviors/connectors for the root context ({rootContextType.FullName})");
            }

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

        static IEnumerable<RegisterStep> Sort(List<RegisterStep> registrations)
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
                Node referencedNode;
                if (nameToNode.TryGetValue(beforeReference.DependsOnId, out referencedNode))
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
                Node referencedNode;
                if (nameToNode.TryGetValue(afterReference.DependsOnId, out referencedNode))
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

        static string GetCurrentIds(Dictionary<string, Node> nameToNodeDict)
        {
            return $"'{string.Join("', '", nameToNodeDict.Keys)}'";
        }

        List<RegisterStep> additions;
        List<RemoveStep> removals;
        List<ReplaceStep> replacements;

        Type rootContextType;
        static CaseInsensitiveIdComparer idComparer = new CaseInsensitiveIdComparer();
        static ILog Logger = LogManager.GetLogger<PipelineModelBuilder>();

        class Node
        {
            public Node(RegisterStep registerStep)
            {
                rego = registerStep;
                Befores = registerStep.Befores;
                Afters = registerStep.Afters;
                StepId = registerStep.StepId;

                OutputContext = registerStep.GetOutputContext();
            }

            public Type OutputContext { get; private set; }

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
                if (rego != null)
                {
                    output.Add(rego);
                }
            }

            public List<Dependency> Afters;
            public List<Dependency> Befores;

            public string StepId;
            internal List<Node> previous = new List<Node>();
            RegisterStep rego;
            bool visited;
        }

        class CaseInsensitiveIdComparer : IEqualityComparer<RemoveStep>
        {
            public bool Equals(RemoveStep x, RemoveStep y)
            {
                return x.RemoveId.Equals(y.RemoveId, StringComparison.CurrentCultureIgnoreCase);
            }

            public int GetHashCode(RemoveStep obj)
            {
                return obj.RemoveId.ToLower().GetHashCode();
            }
        }
    }
}