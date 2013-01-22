namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RunDescriptorsBuilder
    {
        IList<RunDescriptor> descriptors = new List<RunDescriptor>();

        IList<string> excludes = new List<string>(); 
      
        public RunDescriptorsBuilder For<T>() where T:ScenarioDescriptor
        {
            var sd = Activator.CreateInstance<T>() as ScenarioDescriptor;

            if (!descriptors.Any())
            {
                descriptors = sd.ToList();
                return this;
            }
            var result = new List<RunDescriptor>();
                
            foreach (var existingDescriptor in descriptors)
            {
                foreach (var descriptorToAdd in sd.ToList())
                {
                    var nd = new RunDescriptor(existingDescriptor);
                    nd.Merge(descriptorToAdd);
                    result.Add(nd);
                }    
            }

            descriptors = result;

            return this;
        }

        public IList<RunDescriptor> Descriptors
        {
            get
            {
                return descriptors.Where(d => !excludes.Any(e => d.Name.ToLower().Contains(e))).ToList();
            }
        }

        public RunDescriptorsBuilder Except(string nameOfRunToExclude)
        {
            excludes.Add(nameOfRunToExclude.ToLowerInvariant());
            return this;
        }

        public RunDescriptorsBuilder Except(RunDescriptor runToExclude)
        {
            excludes.Add(runToExclude.Name.ToLowerInvariant());
            return this;
        }
    }
}