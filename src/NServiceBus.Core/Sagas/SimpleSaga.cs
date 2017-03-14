namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

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

        /// <summary>
        /// Gets the name of the correlation property for <typeparamref name="TSagaData"/>.
        /// </summary>
        protected abstract string CorrelationPropertyName { get; }

        void VerifyBaseIsSimpleSaga()
        {
            if (!IsBaseSimpleSaga())
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
        /// <see cref="Saga.ConfigureHowToFindSaga"/>.
        /// </summary>
        protected internal override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            VerifyBaseIsSimpleSaga();
            ConfigureMapping(new MessagePropertyMapper<TSagaData>(sagaMessageFindingConfiguration, GetExpression(), GetType()));
        }

        Expression<Func<TSagaData, object>> GetExpression()
        {
            var correlationProperty = GetCorrelationProperty();
            var parameterExpression = Expression.Parameter(typeof(TSagaData));
            var propertyExpression = Expression.Property(parameterExpression, correlationProperty);
            var convert = Expression.Convert(propertyExpression, typeof(object));
            return Expression.Lambda<Func<TSagaData, object>>(convert, parameterExpression);
        }

        PropertyInfo GetCorrelationProperty()
        {
            var correlationProperty = typeof(TSagaData)
                .GetProperty(CorrelationPropertyName, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
            if (correlationProperty != null)
            {
                return correlationProperty;
            }
            var message = $"Expected to find a property named {CorrelationPropertyName} on [{typeof(TSagaData).FullName}].";
            throw new Exception(message);
        }

        /// <summary>
        /// Override this method in order to configure how messages correlate to the property <see cref="CorrelationPropertyName"/> of <typeparamref name="TSagaData"/>.
        /// </summary>
        protected abstract void ConfigureMapping(IMessagePropertyMapper mapper);
    }
}