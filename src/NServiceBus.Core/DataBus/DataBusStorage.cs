namespace NServiceBus.Features
{
    using NServiceBus.DataBus;

    /// <summary>
    ///
    /// </summary>
    public class DataBusStorage : IFeatureService
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="storage"></param>
        public DataBusStorage(IDataBus storage)
        {
            Storage = storage;
        }
        /// <summary>
        ///
        /// </summary>
        public IDataBus Storage { get; private set; }
    }
}