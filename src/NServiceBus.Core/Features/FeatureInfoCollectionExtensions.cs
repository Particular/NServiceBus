#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Features;

static class FeatureInfoCollectionExtensions
{
    extension(IReadOnlyCollection<FeatureComponent.FeatureInfo> featureInfos)
    {
        public ICollection<FeatureComponent.FeatureInfo> Sort()
        {
            // Step 1: create nodes for graph
            var nameToNodeDict = new Dictionary<string, Node>();
            var allNodes = new List<Node>(featureInfos.Count);
            foreach (var featureInfo in featureInfos)
            {
                // create entries to preserve order within
                var node = new Node(featureInfo);

                nameToNodeDict[featureInfo.Name] = node;
                allNodes.Add(node);
            }

            // Step 2: create edges dependencies
            foreach (var node in allNodes)
            {
                foreach (var dependencyName in node.Dependencies.SelectMany(d => d))
                {
                    if (nameToNodeDict.TryGetValue(dependencyName, out var referencedNode))
                    {
                        node.Previous.Add(referencedNode);
                    }
                }
            }

            // Step 3: Perform Topological Sort
            var output = new List<FeatureComponent.FeatureInfo>();
            foreach (var node in allNodes)
            {
                node.Visit(output);
            }

            // Step 4: DFS to check if we have an directed acyclic graph
            foreach (var node in allNodes)
            {
                if (DirectedCycleExistsFrom(node, []))
                {
                    throw new ArgumentException("Cycle in dependency graph detected");
                }
            }

            return output;
        }

#pragma warning disable IDE0051 // Seems an analyzer bug
        static bool DirectedCycleExistsFrom(Node node, Node[] visitedNodes)
        {
            if (node.Previous.Count == 0)
            {
                return false;
            }

            if (visitedNodes.Any(n => n == node))
            {
                return true;
            }

            Node[] newVisitedNodes = [.. visitedNodes, node];
            foreach (var subNode in node.Previous)
            {
                if (DirectedCycleExistsFrom(subNode, newVisitedNodes))
                {
                    return true;
                }
            }
            return false;
        }
    }
#pragma warning restore IDE0051
    sealed class Node(FeatureComponent.FeatureInfo featureInfo)
    {
        public IReadOnlyCollection<IReadOnlyCollection<string>> Dependencies => featureInfo.DependencyNames;
        public List<Node> Previous { get; } = [];

        public void Visit(ICollection<FeatureComponent.FeatureInfo> output)
        {
            if (visited)
            {
                return;
            }
            visited = true;
            foreach (var n in Previous)
            {
                n.Visit(output);
            }
            output.Add(featureInfo);
        }

        bool visited;
    }
}