namespace NServiceBus
{
    using System;
    using Settings;
    using Transport;

    class TransportSettings
    {
        public TransportSettings(SettingsHolder settings)
        {
            this.settings = settings;

            settings.SetDefault(TransportConnectionString.Default);
            settings.Set(new QueueBindings());
        }

        public TransportDefinition TransportDefinition
        {
            get
            {
                if (!settings.HasExplicitValue<TransportDefinition>())
                {
                    throw new Exception("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport.");
                }

                return settings.Get<TransportDefinition>();
            }
            set { settings.Set(value); }
        }

        public TransportConnectionString TransportConnectionString
        {
            get { return settings.Get<TransportConnectionString>(); }
            set { settings.Set(value); }
        }

        public QueueBindings QueueBindings
        {
            get { return settings.Get<QueueBindings>(); }
        }

        public SettingsHolder RawSettings { get { return settings; } }


        readonly SettingsHolder settings;
    }
}