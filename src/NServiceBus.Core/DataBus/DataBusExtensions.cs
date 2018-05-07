namespace NServiceBus.DataBus
{
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// This class provides implementers of databus with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The databus definition eg <see cref="FileShareDataBus" />.</typeparam>
    public class DataBusExtensions<T> : DataBusExtensions where T : DataBusDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DataBusExtensions(SettingsHolder settings)
            : base(settings)
        {
        }
    }

    /// <summary>
    /// This class provides implementers of databus with an extension mechanism for custom settings via extension methods.
    /// </summary>
    public class DataBusExtensions : ExposeSettings
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DataBusExtensions(SettingsHolder settings)
            : base(settings)
        {
        }
    }
}