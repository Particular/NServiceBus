namespace NServiceBus.AcceptanceTesting.Support
{
    public class RunDescriptor
    {
        public RunDescriptor(string key)
        {
            Key = key;
            Settings = new RunSettings();
        }

        public RunDescriptor(RunDescriptor template)
        {
            Settings = new RunSettings();
            Settings.Merge(template.Settings);
            Key = template.Key;
        }

        public string Key { get; private set; }

        public RunSettings Settings { get; }

        public ScenarioContext ScenarioContext { get; set; }

        public int Permutation { get; set; }

        protected bool Equals(RunDescriptor other)
        {
            return string.Equals(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((RunDescriptor) obj);
        }

        public override int GetHashCode()
        {
            return Key?.GetHashCode() ?? 0;
        }

        public void Merge(RunDescriptor descriptorToAdd)
        {
            Key += "." + descriptorToAdd.Key;

            Settings.Merge(descriptorToAdd.Settings);
        }
    }
}