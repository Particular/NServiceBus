namespace NServiceBus.Host
{
    ///<summary>
    /// Interface used to define the behavior of a message endpoint
    /// that will be hosted by the NServiceBus generic host.
    ///</summary>
    public interface IMessageEndpoint
    {
        ///<summary>
        /// Used to define those actions which will be performed when
        /// the service is started.
        ///</summary>
        void OnStart();

        ///<summary>
        /// Used to define those actions which will be performed when
        /// the service is stopped.
        ///</summary>
        void OnStop();
    }
}