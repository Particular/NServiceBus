namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// This class is used to define sagas containing data and handling a message.
    /// To handle more message types, implement <see cref="IHandleMessages{T}" />
    /// for the relevant types.
    /// To signify that the receipt of a message should start this saga,
    /// implement <see cref="IAmStartedByMessages{T}" /> for the relevant message type.
    /// </summary>
    /// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData" />.</typeparam>
    public abstract class SimpleSaga<TSagaData> : Saga where TSagaData : IContainSagaData, new()
    {
        static bool simpleSagaTypeVerified;

        /// <summary>
        /// Initialize a new instance of <see cref="SimpleSaga{TSagaData}"/>
        /// </summary>
        protected SimpleSaga()
        {
            VerifyBaseIsSimpleSaga();
        }

        /// <summary>
        /// Gets the correlation propert expression for <typeparamref name="TSagaData"/>.
        /// </summary>
        protected abstract Expression<Func<TSagaData, object>> CorrelationProperty { get; }

        void VerifyBaseIsSimpleSaga()
        {
            if (simpleSagaTypeVerified)
            {
                return;
            }
            simpleSagaTypeVerified = true;
            if ( !IsBaseSimpleSaga())
            {
                throw new Exception("Implementations of SimpleSaga must inherit directly. Deep class hierarchies are not supported.");
            }
        }

        bool IsBaseSimpleSaga()
        {
            return GetType().BaseType.FullName
                .StartsWith("NServiceBus.SimpleSaga");
        }

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
        /// <see cref="ConfigureHowToFindSaga(MessagePropertyMapper{TSagaData})" />.
        /// </remarks>
        protected internal override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            ConfigureHowToFindSaga(new MessagePropertyMapper<TSagaData>(sagaMessageFindingConfiguration, CorrelationProperty));
        }

        /// <summary>
        /// A generic version of <see cref="ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage)" /> wraps
        /// <see cref="IConfigureHowToFindSagaWithMessage" /> in a generic helper class (
        /// <see cref="MessagePropertyMapper{TSagaData}" />) to provide mappings specific to <typeparamref name="TSagaData" />.
        /// </summary>
        /// <param name="mapper">
        /// The <see cref="MessagePropertyMapper{TSagaData}" /> that wraps the
        /// <see cref="IConfigureHowToFindSagaWithMessage" />.
        /// </param>
        protected abstract void ConfigureHowToFindSaga(MessagePropertyMapper<TSagaData> mapper);
    }
}