namespace NServiceBus.Outbox
{
    using Configuration.AdvanceExtensibility;
    using Settings;

    /// <summary>
    /// Custom settings related to the outbox feature.
    /// </summary>
    public class OutboxSettings : ExposeSettings
    {
        internal OutboxSettings(SettingsHolder settings) : base(settings)
        {
        }
    }
}