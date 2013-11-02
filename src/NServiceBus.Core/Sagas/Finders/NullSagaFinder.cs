namespace NServiceBus.Sagas.Finders
{
    using Saga;

    /// <summary>
    /// Catch-all finder to return null - so that we can later check
    /// for whether a new saga should be created.
    /// </summary>
    [ObsoleteEx(Message = "Not used since 4.2",TreatAsErrorFromVersion = "4.3")]
    public class NullSagaFinder<T> : IFindSagas<T>.Using<object> where T : IContainSagaData
    {
        /// <summary>
        /// Returns null.
        /// </summary>
        public T FindBy(object message)
        {
            return default(T);
        }
    }
}
