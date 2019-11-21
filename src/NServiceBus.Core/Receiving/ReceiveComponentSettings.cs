namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Settings;
    using Transport;
    using Unicast;

    partial class ReceiveComponent
    {
        public class Settings
        {
            public Settings(SettingsHolder settings)
            {
                this.settings = settings;
            }

            public List<Type> ExecuteTheseHandlersFirst => settings.GetOrCreate<List<Type>>();

            public MessageHandlerRegistry MessageHandlerRegistry => settings.GetOrCreate<MessageHandlerRegistry>();

            public bool ShouldCreateQueues
            {
                get => settings.Get<bool>("Transport.CreateQueues");
                set => settings.Set("Transport.CreateQueues", value);
            }

            public bool CustomLocalAddressProvided => settings.HasExplicitValue(ReceiveSettingsExtensions.CustomLocalAddressKey);

            public string CustomLocalAddress => settings.GetOrDefault<string>(ReceiveSettingsExtensions.CustomLocalAddressKey);

            public string EndpointName => settings.EndpointName();

            public string EndpointInstanceDiscriminator => settings.GetOrDefault<string>(EndpointInstanceDiscriminatorSettingsKey);

            public bool UserHasProvidedTransportTransactionMode => settings.HasSetting<TransportTransactionMode>();

            public TransportTransactionMode UserTransportTransactionMode => settings.Get<TransportTransactionMode>();

            public bool PurgeOnStartup
            {
                get => settings.GetOrDefault<bool>(TransportPurgeOnStartupSettingsKey);
                set => settings.Set(TransportPurgeOnStartupSettingsKey, value);
            }

            public PushRuntimeSettings PushRuntimeSettings
            {
                get
                {
                    if (settings.TryGet(out PushRuntimeSettings value))
                    {
                        return value;
                    }

                    return PushRuntimeSettings.Default;
                }
                set => settings.Set(value);
            }

            public Notification<ReceivePipelineCompleted> PipelineCompletedSubscribers => settings.GetOrCreate<Notification<ReceivePipelineCompleted>>();

            public bool IsSendOnlyEndpoint => settings.Get<bool>(EndpointSendOnlySettingKey);

            readonly SettingsHolder settings;

            const string EndpointInstanceDiscriminatorSettingsKey = "EndpointInstanceDiscriminator";
            const string TransportPurgeOnStartupSettingsKey = "Transport.PurgeOnStartup";
            const string EndpointSendOnlySettingKey = "Endpoint.SendOnly";

            public void RegisterReceiveConfigurationForBackwardsCompatibility(Configuration configuration)
            {
                //note: remove once settings.LogicalAddress() , .LocalAddress() and .InstanceSpecificQueue() has been obsoleted
                settings.Set(configuration);
            }

            public void SetDefaultPushRuntimeSettings(PushRuntimeSettings pushRuntimeSettings)
            {
                settings.SetDefault(pushRuntimeSettings);
            }
        }
    }
}