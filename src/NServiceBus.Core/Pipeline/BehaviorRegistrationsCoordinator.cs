namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BehaviorRegistrationsCoordinator
    {
        public void Remove(string idToRemove)
        {
            // I can only remove a behavior that is registered and other behaviors do not depend on, eg InsertBefore/After
            if (String.IsNullOrEmpty(idToRemove))
            {
                throw new ArgumentNullException("idToRemove");
            }

            removals.Add(new RemoveBehavior
            {
                RemoveId = idToRemove
            });
        }

        public void Replace(string idToReplace, Type newBehavior, string description = null)
        {
            if (newBehavior.IsAssignableFrom(iBehaviourType))
            {
                throw new ArgumentException("TBehavior needs to implement IBehavior<TContext>");
            }

            if (String.IsNullOrEmpty(idToReplace))
            {
                throw new ArgumentNullException("idToReplace");
            }

            // If description is null use existing one
            replacements.Add(new ReplaceBehavior
            {
                BehaviorType = newBehavior,
                Description = description,
                ReplaceId = idToReplace
            });
        }

        public void Register(string id, Type behavior, string description)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException("id");
            }

            if (behavior.IsAssignableFrom(iBehaviourType))
            {
                throw new ArgumentException("Needs to implement IBehavior<TContext>", "behavior");
            }

            if (String.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (String.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            additions.Add(RegisterBehavior.Create(id, behavior, description));
        }

        public void Register<T>() where T : RegisterBehavior, new()
        {
            additions.Add(Activator.CreateInstance<T>());
        }

        internal IList<RegisterBehavior> BuildRuntimeModel()
        {
            var registrations = CreateRegistrationsList();

            return Sort(registrations).AsReadOnly();
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
                        listOfBeforeAndAfterIds.AddRange(metadata.Afters);
                    }
                    if (metadata.Befores != null)
                    {
                        listOfBeforeAndAfterIds.AddRange(metadata.Befores);
                    }

                    continue;
                }

                throw new Exception(string.Format("Behavior registration with id '{0}' is already registered for '{1}'", metadata.Id, registrations[metadata.Id].BehaviorType));
            }

            //  Step 2: do replacements
            foreach (var metadata in replacements)
            {
                if (!registrations.ContainsKey(metadata.ReplaceId))
                {
                    throw new Exception(string.Format("You can only replace an existing behavior registration, '{0}' registration does not exist!", metadata.ReplaceId));
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
                    throw new Exception(string.Format("You cannot remove behavior registration with id '{0}', registration does not exist!", metadata.RemoveId));
                }

                if (listOfBeforeAndAfterIds.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase))
                {
                    var add = additions.First(mr => (mr.Befores != null && mr.Befores.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)) ||
                                                    (mr.Afters != null && mr.Afters.Contains(metadata.RemoveId, StringComparer.CurrentCultureIgnoreCase)));

                    throw new Exception(string.Format("You cannot remove behavior registration with id '{0}', registration with id {1} depends on it!", metadata.RemoveId, add.Id));
                }

                registrations.Remove(metadata.RemoveId);
            }

            return registrations.Values;
        }

        static List<RegisterBehavior> Sort(IEnumerable<RegisterBehavior> registrations)
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
                        if (nameToNodeDict.TryGetValue(beforeReference, out referencedNode))
                        {
                            referencedNode.previous.Add(node);
                        }
                        else
                        {
                            Console.WriteLine("Registration ({0}) specified in the insertbefore of the {1} behavior does not exist!", beforeReference, node.Rego.Id);
                        }
                    }
                }

                if (node.Rego.Afters != null)
                {
                    foreach (var afterReference in node.Rego.Afters)
                    {
                        Node referencedNode;
                        if (nameToNodeDict.TryGetValue(afterReference, out referencedNode))
                        {
                            node.previous.Add(referencedNode);
                        }
                        else
                        {
                            Console.WriteLine("Registration ({0}) specified in the insertafter of the {1} behavior does not exist!", afterReference, node.Rego.Id);
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

        static Type iBehaviourType = typeof(IBehavior<>);
        List<RegisterBehavior> additions = new List<RegisterBehavior>();
        List<RemoveBehavior> removals = new List<RemoveBehavior>();
        List<ReplaceBehavior> replacements = new List<ReplaceBehavior>();

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