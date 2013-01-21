namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System.Collections.Generic;
    using System.Linq;

    public class RunDescriptor
    {
        public RunDescriptor()
        {
            Settings = new Dictionary<string, string>();
        }

        public RunDescriptor(RunDescriptor template)
        {
            Settings = template.Settings.ToDictionary(entry => entry.Key,
                                                      entry => entry.Value);
            Name = template.Name;
        }

        public string Name { get; set; }

        public IDictionary<string, string> Settings { get; set; }

        public void Merge(RunDescriptor descriptorToAdd)
        {
            Name += " | " + descriptorToAdd.Name;

            foreach (var setting in descriptorToAdd.Settings)
            {
                Settings[setting.Key] = setting.Value;
            }
        }
    }
}