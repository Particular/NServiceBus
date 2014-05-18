namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;

    public class ConfigurationContext : BehaviorContext
    {
        readonly IList<Type> typesToScan;

        public ConfigurationContext(IList<Type> typesToScan)
            : base(null)
        {
            this.typesToScan = typesToScan;
        }

        public Configure Configure
        {
            get { return Get<Configure>(); }
        }

        public IList<Type> TypesToScan { get { return typesToScan; } }
    }
}