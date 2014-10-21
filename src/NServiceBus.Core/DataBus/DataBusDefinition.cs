namespace NServiceBus.DataBus
{
    using System;

    /// <summary>
    /// Defines a data bus that can be used by NServiceBus
    /// </summary>
    public abstract class DataBusDefinition
    {
        /// <summary>
        /// Type of feature that configures a specific data bus type
        /// </summary>
        public Type DataBusFeatureType { get; set; }
    }
}