namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;

    class PipelineModelBuilder
    {
        public PipelineModelBuilder(Type rootContextType,List<RegisterStep> additions, List<RemoveStep> removals, List<ReplaceBehavior> replacements)
        {
            this.rootContextType = rootContextType;
            this.additions = additions;
            this.removals = removals;
            this.replacements = replacements;
        }

        public IList<RegisterStep> Build()
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

                var message = string.Format("Step registration with id '{0}' is already registered for '{1}'", metadata.StepId, registrations[metadata.StepId].BehaviorType);
                throw new Exception(message);
            }

            //  Step 2: do replacements
            foreach (var metadata in replacements)
            {
                if (!registrations.ContainsKey(metadata.ReplaceId))
                {
                    var message = string.Format("You can only replace an existing step registration, '{0}' registration does not exist!", metadata.ReplaceId);
                    throw new Exception(message);
                }

                registrations[metadata.ReplaceId].BehaviorType = metadata.BehaviorType;
                if (!String.IsNullOrEmpty(metadata.Description))
                {
                    registrations[metadata.ReplaceId].Description = metadata.Description;
                }
            }

            // Step 3: validate the removals
            foreach (var metadata in removals.Distinct(new CaseInsensitiveIdComparer()))
            {
                if (!registrations.ContainsKey(metadata.RemoveId))
                {
                    var message = string.Format("You cannot remove step registration with id '{0}', registration does not exist!", metadata.RemoveId);
                    throw new Exception(message);
                }

                if (listOfBeforeAndAfterIds.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase))
                {
                    var add = additions.First(mr => (mr.Befores != null && mr.Befores.Select(b => b.DependsOnId).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)) ||
                                                    (mr.Afters != null && mr.Afters.Select(b => b.DependsOnId).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)));

                    var message = string.Format("You cannot remove step registration with id '{0}', registration with id {1} depends on it!", metadata.RemoveId, add.StepId);
                    throw new Exception(message);
                }

                registrations.Remove(metadata.RemoveId);
            }

            var stages = registrations.Values.GroupBy(r => r.GetInputContext())
                .ToList();



            var finalOrder = new List<RegisterStep>();

            if (!registrations.Any())
            {
                return finalOrder;
            }

            //todo: add test and better ex for missing start stage
            var currentStage = stages.SingleOrDefault(stage => stage.Key == rootContextType);

            if (currentStage == null)
            {
                throw new Exception(string.Format("Can't find any behaviors/connectors for the root context ({0})",rootContextType.FullName));
            }

            var stageNumber = 1;

            while (currentStage != null)
            {
                var stageSteps = currentStage.Where(stageStep => !IsStageConnector(stageStep)).ToList();

                //add the stage connector
                finalOrder.AddRange(Sort(stageSteps));


                //todo: add tests and better ex for missing stage connector
                var stageConnectors = currentStage.Where(IsStageConnector).ToList();

                if (stageConnectors.Count() > 1)
                {
                    throw new Exception(string.Format("Multiple stage connectors found for stage {0}. Please remove one of: {1}", currentStage.Key.FullName,string.Join(";",stageConnectors.Select(sc=>sc.BehaviorType.FullName))));
                }

                var stageConnector = stageConnectors.FirstOrDefault();

                if (stageConnector == null)
                {
                    if (stageNumber < stages.Count())
                    {
                        throw new Exception(string.Format("No stage connector found for stage {0}", currentStage.Key.FullName));    
                    }

                    currentStage = null;
                }
                else
                {
                    finalOrder.Add(stageConnector);

                    var args = stageConnector.BehaviorType.BaseType.GetGenericArguments();
                    var stageEndType = args[1];

                    currentStage = stages.SingleOrDefault(stage => stage.Key == stageEndType);         
                }

                stageNumber++;

            }

            return finalOrder;
        }

        static bool IsStageConnector(RegisterStep stageStep)
        {
            return typeof(IStageConnector).IsAssignableFrom(stageStep.BehaviorType);
        }

        static IEnumerable<RegisterStep> Sort(IList<RegisterStep> registrations)
        {

            if (!registrations.Any())
            {
                return registrations;
            }


            // Step 1: create nodes for graph
            var nameToNodeDict = new Dictionary<string, Node>();
            var allNodes = new List<Node>();
            foreach (var rego in registrations)
            {
                // create entries to preserve order within
                var node = new Node(rego);
                nameToNodeDict[rego.StepId] = node;
                allNodes.Add(node);
            }

            // Step 2: create edges from InsertBefore/InsertAfter values
            foreach (var node in allNodes)
            {
                if (node.Befores != null)
                {
                    foreach (var beforeReference in node.Befores)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(beforeReference.DependsOnId, out referencedNode))
                        {
                            referencedNode.previous.Add(node);
                        }
                        else
                        {
                            var message = string.Format("Registration '{0}' specified in the insertbefore of the '{1}' step does not exist in this stage!", beforeReference.DependsOnId, node.StepId);

                            if (!beforeReference.Enforce)
                            {
                                Logger.Info(message);
                            }
                            else
                            {
                                throw new Exception(message);
                            }
                        }
                    }
                }

                if (node.Afters != null)
                {
                    foreach (var afterReference in node.Afters)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(afterReference.DependsOnId, out referencedNode))
                        {
                            node.previous.Add(referencedNode);
                        }
                        else
                        {
                            var message = string.Format("Registration '{0}' specified in the insertafter of the '{1}' step does not exist!", afterReference.DependsOnId, node.StepId);

                            if (!afterReference.Enforce)
                            {
                                Logger.Info(message);
                            }
                            else
                            {
                                throw new Exception(message);
                            }
                        }
                    }
                }
            }

            // Step 3: Perform Topological Sort
            var output = new List<RegisterStep>();
            foreach (var node in allNodes)
            {
                node.Visit(output);
            }

            // Step 4: Validate intput and output types
            for (var i = 1; i < output.Count; i++)
            {
                var previousBehavior = output[i - 1].BehaviorType;
                var thisBehavior = output[i].BehaviorType;

                var incomingType = previousBehavior.GetOutputContext();
                var inputType = thisBehavior.GetInputContext();

                if (!inputType.IsAssignableFrom(incomingType))
                {
                    throw new Exception(string.Format("Cannot chain behavior {0} and {1} together because output type of behavior {0} ({2}) cannot be passed as input for behavior {1} ({3})",
                        previousBehavior.FullName,
                        thisBehavior.FullName,
                        incomingType,
                        inputType));
                }
            }
            return output;
        }

        readonly Type rootContextType;
        readonly List<RegisterStep> additions;
        readonly List<RemoveStep> removals;
        readonly List<ReplaceBehavior> replacements;
        static ILog Logger = LogManager.GetLogger<PipelineModelBuilder>();


        class Node
        {
            internal void Visit(ICollection<RegisterStep> output)
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

            public Node(RegisterStep registerStep)
            {
                rego = registerStep;
                Befores = registerStep.Befores;
                Afters = registerStep.Afters;
                StepId = registerStep.StepId;

                OutputContext = registerStep.GetOutputContext();
            }

            public readonly string StepId;
            private readonly RegisterStep rego;
            public readonly IList<Dependency> Befores;
            public readonly IList<Dependency> Afters;
            internal List<Node> previous = new List<Node>();
            bool visited;
            public Type OutputContext { get; private set; }
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

    static class RegisterStepExtensions
    {
        public static bool IsStageConnector(this RegisterStep step)
        {
            return typeof(IStageConnector).IsAssignableFrom(step.BehaviorType);
        }

        public  static Type GetContextType(this Type behaviorType)
        {
            var behaviorInterface = behaviorType.GetBehaviorInterface();

            var type = behaviorInterface.GetGenericArguments()[0];

            return type;
        }
        public static Type GetBehaviorInterface(this Type behaviorType)
        {
            var behaviorInterface = behaviorType.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            return behaviorInterface;
        }

        public static Type GetOutputContext(this RegisterStep step)
        {
            return step.BehaviorType.GetOutputContext();
        }

        public static Type GetOutputContext(this Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);

            var type = behaviorInterface.GetGenericArguments()[1];

            return type;
        }

        public static Type GetInputContext(this RegisterStep step)
        {
            return step.BehaviorType.GetInputContext();
        }

        public static Type GetInputContext(this Type behaviorType)
        {
            var behaviorInterface = GetBehaviorInterface(behaviorType);

            var type = behaviorInterface.GetGenericArguments()[0];

            return type;
        }

    }
}