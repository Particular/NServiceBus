namespace NServiceBus
{
    using NServiceBus.DataBus;

    /// <summary>
    /// Base class for data bus definitions
    /// </summary>
    public class FileShareDataBus : DataBusDefinition
    {
        /// <summary>
        /// Creates a new instance of <see cref="FileShareDataBusInternal"/>
        /// </summary>
        public FileShareDataBus()
        {
            DataBusFeatureType = typeof(Features.DataBus);
        }
    }
}