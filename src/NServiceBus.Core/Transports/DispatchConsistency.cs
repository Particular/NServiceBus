namespace NServiceBus.Transport
{
    /// <summary>
    /// The requested level of dispatch consistency.
    /// </summary>
    public enum DispatchConsistency
    {
        /// <summary>
        /// The transport should use it's default mode when deciding to enlist the dispatch operation in the receive transaction.
        /// </summary>
        Default = 1,

        /// <summary>
        /// The message should be dispatched immediately without enlisting in any ongoing receive transaction.
        /// </summary>
        Isolated = 2
    }
}