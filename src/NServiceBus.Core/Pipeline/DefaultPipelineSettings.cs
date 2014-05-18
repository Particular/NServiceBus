namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Settings;

    class DefaultPipelineSettings : ISetDefaultSettings
    {
        public DefaultPipelineSettings()
        {
            SettingsHolder.Instance.SetDefault("Pipeline.Removals", new List<RemoveBehavior>());
            SettingsHolder.Instance.SetDefault("Pipeline.Replacements", new List<ReplaceBehavior>());
            SettingsHolder.Instance.SetDefault("Pipeline.Additions", new List<RegisterBehavior>());
        }
    }
}