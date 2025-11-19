namespace NServiceBus;

using Settings;
using Transport;
using Unicast;

partial class ReceiveComponent
{
    public class Settings(SettingsHolder settings)
    {
        public MessageHandlerRegistry MessageHandlerRegistry => settings.GetOrCreate<MessageHandlerRegistry>();

        public bool CustomQueueNameBaseProvided => settings.HasExplicitValue(ReceiveSettingsExtensions.CustomQueueNameBaseKey);

        public string CustomQueueNameBase => settings.GetOrDefault<string>(ReceiveSettingsExtensions.CustomQueueNameBaseKey);

        public string EndpointName => settings.EndpointName();

        public string EndpointInstanceDiscriminator => settings.GetOrDefault<string>(EndpointInstanceDiscriminatorSettingsKey);

        public bool PurgeOnStartup
        {
            get => settings.GetOrDefault<bool>(TransportPurgeOnStartupSettingsKey);
            set => settings.Set(TransportPurgeOnStartupSettingsKey, value);
        }

        public PushRuntimeSettings PushRuntimeSettings
        {
            get => settings.TryGet(out PushRuntimeSettings value) ? value : PushRuntimeSettings.Default;
            set => settings.Set(value);
        }

        public Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers => settings.GetOrCreate<Notification<ReceivePipelineCompleted>>();

        public bool IsSendOnlyEndpoint => settings.Get<bool>(EndpointSendOnlySettingKey);

        public void RegisterReceiveConfigurationForBackwardsCompatibility(Configuration configuration) =>
            //note: remove once settings.LogicalAddress() , .LocalAddress() and .InstanceSpecificQueue() has been obsoleted
            settings.Set(configuration);

        const string EndpointInstanceDiscriminatorSettingsKey = "EndpointInstanceDiscriminator";
        const string TransportPurgeOnStartupSettingsKey = "Transport.PurgeOnStartup";
        const string EndpointSendOnlySettingKey = "Endpoint.SendOnly";
    }
}