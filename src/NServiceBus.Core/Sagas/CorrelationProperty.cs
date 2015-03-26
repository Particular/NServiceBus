namespace NServiceBus.Saga
{
    /// <summary>
    /// Details about a saga data property used to correlate messages hitting the saga
    /// </summary>
    public class CorrelationProperty
    {
        /// <summary>
        /// The name of the saga data property
        /// </summary>
        public string Name;
    }
}