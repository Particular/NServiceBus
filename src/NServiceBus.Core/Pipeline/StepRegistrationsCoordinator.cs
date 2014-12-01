namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    class StepRegistrationsCoordinator
    {
        public StepRegistrationsCoordinator(List<RemoveStep> removals, List<ReplaceBehavior> replacements)
        {
            this.removals = removals;
            this.replacements = replacements;
        }

        public void Register(WellKnownStep wellKnownStep, Type behavior, string description)
        {
            additions.Add(RegisterStep.Create(wellKnownStep, behavior, description));
        }

        public void Register(RegisterStep rego)
        {
            additions.Add(rego);
        }

        public IEnumerable<RegisterStep> BuildRuntimeModel()
        {
            var registrations = CreateRegistrationsList();

            return Sort(registrations);
        }

        IEnumerable<RegisterStep> CreateRegistrationsList()
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
                        listOfBeforeAndAfterIds.AddRange(metadata.Afters.Select(a=>a.Id));
                    }
                    if (metadata.Befores != null)
                    {
                        listOfBeforeAndAfterIds.AddRange(metadata.Befores.Select(b=>b.Id));
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
                    var add = additions.First(mr => (mr.Befores != null && mr.Befores.Select(b=>b.Id).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)) ||
                                                    (mr.Afters != null && mr.Afters.Select(b=>b.Id).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)));

                    var message = string.Format("You cannot remove step registration with id '{0}', registration with id {1} depends on it!", metadata.RemoveId, add.StepId);
                    throw new Exception(message);
                }

                registrations.Remove(metadata.RemoveId);
            }

            return registrations.Values;
        }

        static IEnumerable<RegisterStep> Sort(IEnumerable<RegisterStep> registrations)
        {
            // Step 1: create nodes for graph
            var nameToNodeDict = new Dictionary<string, Node>();
            var allNodes = new List<Node>();
            foreach (var rego in registrations)
            {
                // create entries to preserve order within
                var node = new Node
                {
                    Rego = rego
                };
                nameToNodeDict[rego.StepId] = node;
                allNodes.Add(node);
            }

            // Step 2: create edges from InsertBefore/InsertAfter values
            foreach (var node in allNodes)
            {
                if (node.Rego.Befores != null)
                {
                    foreach (var beforeReference in node.Rego.Befores)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(beforeReference.Id, out referencedNode))
                        {
                            referencedNode.previous.Add(node);
                        }
                        else
                        {
                            var message = string.Format("Registration '{0}' specified in the insertbefore of the '{1}' step does not exist!", beforeReference.Id, node.Rego.StepId);

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

                if (node.Rego.Afters != null)
                {
                    foreach (var afterReference in node.Rego.Afters)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(afterReference.Id, out referencedNode))
                        {
                            node.previous.Add(referencedNode);
                        }
                        else
                        {
                            var message = string.Format("Registration '{0}' specified in the insertafter of the '{1}' step does not exist!", afterReference.Id, node.Rego.StepId);

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

            return output;
        }

        List<RegisterStep> additions = new List<RegisterStep>();
        List<RemoveStep> removals;
        List<ReplaceBehavior> replacements;

        static ILog Logger = LogManager.GetLogger<StepRegistrationsCoordinator>();

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
                output.Add(Rego);
            }

            internal RegisterStep Rego;
            internal List<Node> previous = new List<Node>();
            bool visited;
        }

        public void Register(string pipelineStep, Type behavior, string description)
        {
            Register(WellKnownStep.Create(pipelineStep), behavior, description);
        }
    }
}