namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RunDescriptorsBuilder
    {
        IList<RunDescriptor> descriptors = new List<RunDescriptor>();

        readonly List<string> excludes = new List<string>();
        public RunDescriptorsBuilder For<T>(params RunDescriptor[] runDescriptorsToExclude) where T : ScenarioDescriptor
        {
            excludes.AddRange(runDescriptorsToExclude
                .Where(r=>r != null)
                .Select(r =>r.Key.ToLowerInvariant()).ToArray());

            var sd = Activator.CreateInstance<T>() as ScenarioDescriptor;

            return For(sd.ToArray());
        }

        public RunDescriptorsBuilder For(params RunDescriptor[] descriptorsToAdd)
        {
            var toAdd = descriptorsToAdd.Where(r => r != null).ToList();

            if (!toAdd.Any())
            {
                emptyPermutationFound = true;
            }

            if (!descriptors.Any())
            {
                descriptors = toAdd;
                return this;
            }


            var result = new List<RunDescriptor>();

            foreach (var existingDescriptor in descriptors)
            {
                foreach (var descriptorToAdd in toAdd)
                {
                    var nd = new RunDescriptor(existingDescriptor);
                    nd.Merge(descriptorToAdd);
                    result.Add(nd);
                }
            }


            descriptors = result;

            return this;
        }

        public IList<RunDescriptor> Build()
        {
            //if we have found a empty permutation this means that we shouldn't run any permutations. This happens when a test is specified to run for a given key
            // but that key is not available. Eg running tests for sql server but the sql transport isn't available
            if (emptyPermutationFound)
            {
                return new List<RunDescriptor>();
            }

            var environmentExcludes = GetEnvironmentExcludes();

            var activeDescriptors = descriptors.Where(d =>
                !excludes.Any(e => d.Key.ToLower().Contains(e)) &&
                !environmentExcludes.Any(e => d.Key.ToLower().Contains(e))
                ).ToList();

            var permutation = 1;
            foreach (var run in activeDescriptors)
            {
                run.Permutation = permutation;

                permutation++;

            }

            return activeDescriptors;
        }

        static IList<string> GetEnvironmentExcludes()
        {
            var env = Environment.GetEnvironmentVariable("nservicebus_test_exclude_scenarios");
            if (string.IsNullOrEmpty(env))
                return new List<string>();

            Console.Out.WriteLine("Scenarios excluded for this environment: " + env);
            return env.ToLower().Split(';');
        }

        bool emptyPermutationFound;

        
    }
}