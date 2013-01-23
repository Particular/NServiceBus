namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RunDescriptorsBuilder
    {
        IList<RunDescriptor> descriptors = new List<RunDescriptor>();

        IList<string> excludes = new List<string>();

        public RunDescriptorsBuilder For<T>() where T : ScenarioDescriptor
        {
            var sd = Activator.CreateInstance<T>() as ScenarioDescriptor;

            return For(sd.ToList());
        }


        public RunDescriptorsBuilder For(RunDescriptor descriptor)
        {
            return For(new[] {descriptor});
        }

        public RunDescriptorsBuilder For(IEnumerable<RunDescriptor> descriptorsToAdd)
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

            int permutation = 1;
            foreach (var run in result)
            {
                run.Permutation = permutation;

                permutation++;

            }

            descriptors = result;

            return this;
        }
        
       

        public IList<RunDescriptor> Descriptors
        {
            get
            {
                return descriptors.Where(d => !excludes.Any(e => d.Key.ToLower().Contains(e))).ToList();
            }
        }

        public RunDescriptorsBuilder Except(string nameOfRunToExclude)
        {
            excludes.Add(nameOfRunToExclude.ToLowerInvariant());
            return this;
        }

        public RunDescriptorsBuilder Except(RunDescriptor runToExclude)
        {
            excludes.Add(runToExclude.Key.ToLowerInvariant());
            return this;
        }
    }
}