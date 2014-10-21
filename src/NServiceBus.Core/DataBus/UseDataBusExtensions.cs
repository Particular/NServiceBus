namespace NServiceBus.DataBus
{
    using System;

    /// <summary>
    /// Extension methods to configure data bus
    /// </summary>
    public static class UseDataBusExtensions
    {
        private const string DataBusDefinitionType = "dataBusDefinitionType";

        /// <summary>
        /// Configures NServiceBus to use the given data bus.
        /// </summary>
        public static void UseDataBus<T>(this BusConfiguration busConfiguration) where T : DataBusDefinition, new()
        {
            UseDataBus(busConfiguration, typeof(T));
        }

        /// <summary>
        /// Configures NServiceBus to use the given data bus.
        /// </summary>
        public static void UseDataBus(this BusConfiguration busConfiguration, Type dataBusDefinitionType)
        {
            busConfiguration.Settings.Set(DataBusDefinitionType, dataBusDefinitionType);

            var definition = (DataBusDefinition)Activator.CreateInstance(dataBusDefinitionType);
            busConfiguration.EnableFeature(definition.DataBusFeatureType);
        }
    }
}
