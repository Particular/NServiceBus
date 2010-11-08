namespace NServiceBus.Saga
{
    /// <summary>
    /// Implementers of ISaga should implement this interface as well if they want
    /// initialization time configuration.
    /// </summary>
    public interface IConfigurable
    {
        /// <summary>
        /// Called by the infrastructure to give a chance for initialization time configuration of the saga.
        /// </summary>
        void Configure();
    }
}
