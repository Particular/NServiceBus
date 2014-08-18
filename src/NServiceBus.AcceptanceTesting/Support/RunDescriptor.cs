namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RunDescriptor
    {
        protected bool Equals(RunDescriptor other)
        {
            return string.Equals(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RunDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return (Key != null ? Key.GetHashCode() : 0);
        }

        public RunDescriptor()
        {
            Settings = new Dictionary<string, string>();
        }

        public RunDescriptor(RunDescriptor template)
        {
            Settings = template.Settings.ToDictionary(entry => entry.Key,
                                                      entry => entry.Value);
            Key = template.Key;
        }

        public string Key { get; set; }

        public IDictionary<string, string> Settings { get; set; }

        public ScenarioContext ScenarioContext { get; set; }

        public TimeSpan TestExecutionTimeout { get; set; }

        public int Permutation { get; set; }

        public void Merge(RunDescriptor descriptorToAdd)
        {
            Key += "." + descriptorToAdd.Key;

            foreach (var setting in descriptorToAdd.Settings)
            {
                Settings[setting.Key] = setting.Value;
            }
        }
    }
}