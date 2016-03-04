namespace NServiceBus
{
    /// <summary>
    /// This class is used to define sagas containing data and handling a message.
    /// To handle more message types, implement <see cref="IHandleMessages{T}" />
    /// for the relevant types.
    /// To signify that the receipt of a message should start this saga,
    /// implement <see cref="IAmStartedByMessages{T}" /> for the relevant message type.
    /// </summary>
    /// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData" />.</typeparam>
    public abstract class Saga<TSagaData> : Saga where TSagaData : IContainSagaData, new()
    {
        /// <summary>
        /// The saga's strongly typed data. Wraps <see cref="Saga.Entity" />.
        /// </summary>
        public TSagaData Data
        {
            get { return (TSagaData) Entity; }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                Entity = value;
            }
        }


        /// <summary>
        /// Override this method in order to configure how this saga's data should be found.
        /// </summary>
        /// <remarks>
        /// Override <see cref="Saga.ConfigureHowToFindSaga" /> and forwards it to the generic version
        /// <see cref="ConfigureHowToFindSaga(SagaPropertyMapper{TSagaData})" />.
        /// </remarks>
        internal protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            ConfigureHowToFindSaga(new SagaPropertyMapper<TSagaData>(sagaMessageFindingConfiguration));
        }

        /// <summary>
        /// A generic version of <see cref="ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage)" /> wraps
        /// <see cref="IConfigureHowToFindSagaWithMessage" /> in a generic helper class (
        /// <see cref="SagaPropertyMapper{TSagaData}" />) to provide mappings specific to <typeparamref name="TSagaData" />.
        /// </summary>
        /// <param name="mapper">
        /// The <see cref="SagaPropertyMapper{TSagaData}" /> that wraps the
        /// <see cref="IConfigureHowToFindSagaWithMessage" />.
        /// </param>
        protected abstract void ConfigureHowToFindSaga(SagaPropertyMapper<TSagaData> mapper);
    }
}