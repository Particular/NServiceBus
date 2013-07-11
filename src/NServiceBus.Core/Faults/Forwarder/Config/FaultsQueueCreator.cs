namespace NServiceBus.Faults.Forwarder.Config
{
    using Unicast.Queuing;

    /// <summary>
    /// Signals to create faults queue
    /// </summary>
    public class FaultsQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Signals to create the faults queue
        /// </summary>
        public Address Address
        {
            get { return ConfigureFaultsForwarder.ErrorQueue; }
        }

        /// <summary>
        /// Disabling the creation of faults queue
        /// </summary>
        public bool IsDisabled
        {
            get { return ConfigureFaultsForwarder.ErrorQueue == null; }
        }
    }
}
