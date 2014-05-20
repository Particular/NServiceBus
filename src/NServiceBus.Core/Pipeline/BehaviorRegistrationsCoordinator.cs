namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    class BehaviorRegistrationsCoordinator
    {
        public BehaviorRegistrationsCoordinator(List<RemoveBehavior> removals, List<ReplaceBehavior> replacements)
        {
            this.removals = removals;
            this.replacements = replacements;
        }

        public void Register(string id, Type behavior, string description)
        {
            additions.Add(RegisterBehavior.Create(id, behavior, description));
        }

        public void Register(RegisterBehavior rego)
        {
            additions.Add(rego);
        }

        public IEnumerable<RegisterBehavior> BuildRuntimeModel()
        {
            var registrations = CreateRegistrationsList();

            return Sort(registrations);
        }

        IEnumerable<RegisterBehavior> CreateRegistrationsList()
        {
            var registrations = new Dictionary<string, RegisterBehavior>(StringComparer.CurrentCultureIgnoreCase);
            var listOfBeforeAndAfterIds = new List<string>();

            // Let's do some validation too

            //Step 1: validate that additions are unique
            foreach (var metadata in additions)
            {
                if (!registrations.ContainsKey(metadata.Id))
                {
                    registrations.Add(metadata.Id, metadata);
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

                var message = string.Format("Behavior registration with id '{0}' is already registered for '{1}'", metadata.Id, registrations[metadata.Id].BehaviorType);
                throw new Exception(message);
            }

            //  Step 2: do replacements
            foreach (var metadata in replacements)
            {
                if (!registrations.ContainsKey(metadata.ReplaceId))
                {
                    var message = string.Format("You can only replace an existing behavior registration, '{0}' registration does not exist!", metadata.ReplaceId);
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
                    var message = string.Format("You cannot remove behavior registration with id '{0}', registration does not exist!", metadata.RemoveId);
                    throw new Exception(message);
                }

                if (listOfBeforeAndAfterIds.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase))
                {
                    var add = additions.First(mr => (mr.Befores != null && mr.Befores.Select(b=>b.Id).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)) ||
                                                    (mr.Afters != null && mr.Afters.Select(b=>b.Id).Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)));

                    var message = string.Format("You cannot remove behavior registration with id '{0}', registration with id {1} depends on it!", metadata.RemoveId, add.Id);
                    throw new Exception(message);
                }

                registrations.Remove(metadata.RemoveId);
            }

            return registrations.Values;
        }

        static IEnumerable<RegisterBehavior> Sort(IEnumerable<RegisterBehavior> registrations)
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
                nameToNodeDict[rego.Id] = node;
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
                            var message = string.Format("Registration '{0}' specified in the insertbefore of the '{1}' behavior does not exist!", beforeReference.Id, node.Rego.Id);

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
                            var message = string.Format("Registration '{0}' specified in the insertafter of the '{1}' behavior does not exist!", afterReference.Id, node.Rego.Id);

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
            var output = new List<RegisterBehavior>();
            foreach (var node in allNodes)
            {
                node.Visit(output);
            }

            return output;
        }

        List<RegisterBehavior> additions = new List<RegisterBehavior>();
        List<RemoveBehavior> removals;
        List<ReplaceBehavior> replacements;

        static readonly ILog Logger = LogManager.GetLogger(typeof(BehaviorRegistrationsCoordinator));

        class CaseInsensitiveIdComparer : IEqualityComparer<RemoveBehavior>
        {
            public bool Equals(RemoveBehavior x, RemoveBehavior y)
            {
                return x.RemoveId.Equals(y.RemoveId, StringComparison.CurrentCultureIgnoreCase);
            }

            public int GetHashCode(RemoveBehavior obj)
            {
                return obj.RemoveId.ToLower().GetHashCode();
            }
        }

        class Node
        {
            internal void Visit(ICollection<RegisterBehavior> output)
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

            internal RegisterBehavior Rego;
            internal List<Node> previous = new List<Node>();
            bool visited;
        }
    }
}