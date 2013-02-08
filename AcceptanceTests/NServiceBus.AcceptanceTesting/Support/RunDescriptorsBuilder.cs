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
            excludes.AddRange(runDescriptorsToExclude.Select(r => r.Key.ToLowerInvariant()).ToArray());

            var sd = Activator.CreateInstance<T>() as ScenarioDescriptor;

            return For(sd.ToArray());
        }

        public RunDescriptorsBuilder For(params RunDescriptor[] descriptorsToAdd)
        {
            if (!descriptors.Any())
            {
                descriptors = descriptorsToAdd.ToList();
                return this;
            }


            var result = new List<RunDescriptor>();

            foreach (var existingDescriptor in descriptors)
            {
                foreach (var descriptorToAdd in descriptorsToAdd.ToList())
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
            var environmentExcludes = GetEnvironmentExcludes();

            var activeDescriptors = descriptors.Where(d =>
                !excludes.Any(e => d.Key.ToLower().Contains(e)) &&
                !environmentExcludes.Any(e => d.Key.ToLower().Contains(e))
                ).ToList();

            int permutation = 1;
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
    }
}