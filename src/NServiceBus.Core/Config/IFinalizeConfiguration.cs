namespace NServiceBus.Config
{
    /// <summary>
    /// Interface used to finalize configuration. This is the final point where the container can be altered.
    /// </summary>
    public interface IFinalizeConfiguration
    {
        /// <summary>
        /// Invoked by the framework when the configuration is to be finalized
        /// </summary>
        void FinalizeConfiguration();
    }
}