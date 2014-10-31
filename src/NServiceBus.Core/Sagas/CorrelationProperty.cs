namespace NServiceBus.Sagas
{
    /// <summary>
    /// Details about a saga data property used to correlate messages hitting the saga
    /// </summary>
    class CorrelationProperty
    {
        /// <summary>
        /// The name of the saga data property
        /// </summary>
        public string Name;
    }
}