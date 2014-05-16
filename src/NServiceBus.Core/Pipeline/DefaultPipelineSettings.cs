namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Settings;

    internal class DefaultPipelineSettings : ISetDefaultSettings
    {
        public DefaultPipelineSettings()
        {
            SettingsHolder.SetDefault("Pipeline.Removals", new List<RemoveBehavior>());
            SettingsHolder.SetDefault("Pipeline.Replacements", new List<ReplaceBehavior>());
            SettingsHolder.SetDefault("Pipeline.Additions", new List<RegisterBehavior>());
        }
    }
}