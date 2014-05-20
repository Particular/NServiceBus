namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;

    class DefaultPipelineSettings : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
            configure.Settings.SetDefault("Pipeline.Removals", new List<RemoveBehavior>());
            configure.Settings.SetDefault("Pipeline.Replacements", new List<ReplaceBehavior>());
            configure.Settings.SetDefault("Pipeline.Additions", new List<RegisterBehavior>());
        }
    }
}