namespace NServiceBus.DataBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides implementers of databus with an extension mechanism for custom settings via extention methods.
    /// </summary>
    /// <typeparam name="T">The databus definition eg <see cref="FileShareDataBus"/>.</typeparam>
    public class DataBusExtentions<T> : DataBusExtentions where T : DataBusDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DataBusExtentions(SettingsHolder settings)
            : base(settings)
        {
        }
    }

    /// <summary>
    /// This class provides implementers of databus with an extension mechanism for custom settings via extention methods.
    /// </summary>
    public class DataBusExtentions : ExposeSettings
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DataBusExtentions(SettingsHolder settings)
            : base(settings)
        {
        }
    }
}