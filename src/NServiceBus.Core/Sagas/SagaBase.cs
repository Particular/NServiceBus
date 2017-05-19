namespace NServiceBus
{
    /// <summary>
    /// The base class for sagas, persisted objects that can interact with messages.
    /// </summary>
    public abstract class SagaBase
    {
        /// <summary>
        /// The saga's typed data.
        /// </summary>
        public IContainSagaData Entity { get; set; }

        /// <summary>
        /// Indicates that the saga is complete.
        /// In order to set this value, use the <see cref="MarkAsComplete" /> method.
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Override this method in order to configure how this saga's data should be found.
        /// </summary>
        protected internal abstract void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration);

        /// <summary>
        /// Marks the saga as complete.
        /// This may result in the sagas state being deleted by the persister.
        /// </summary>
        protected void MarkAsComplete()
        {
            Completed = true;
        }
    }
}