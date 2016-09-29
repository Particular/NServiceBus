namespace NServiceBus.Sagas
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Represents a saga instance being processed on the pipeline.
    /// </summary>
    public class ActiveSagaInstance
    {
        readonly Func<DateTime> currentUtcDateTimeProvider;

        internal ActiveSagaInstance(Saga saga, SagaMetadata metadata, Func<DateTime> currentUtcDateTimeProvider)
        {
            this.currentUtcDateTimeProvider = currentUtcDateTimeProvider;
            Instance = saga;
            Metadata = metadata;

            Created = currentUtcDateTimeProvider();
            Modified = Created;
        }

        /// <summary>
        /// The id of the saga.
        /// </summary>
        public string SagaId { get; private set; }

        /// <summary>
        /// The type of the saga.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = ".Metadata.SagaType")]
        public Type SagaType
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Metadata for this active saga.
        /// </summary>
        internal SagaMetadata Metadata { get; }

        /// <summary>
        /// The actual saga instance.
        /// </summary>
        public Saga Instance { get; }

        /// <summary>
        /// True if this saga was created by this incoming message.
        /// </summary>
        public bool IsNew { get; private set; }

        /// <summary>
        /// True if no saga instance could be found for this message.
        /// </summary>
        public bool NotFound { get; private set; }

        /// <summary>
        /// UTC timestamp of when the active saga instance was created.
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// UTC timestamp of when the active saga instance was last modified.
        /// </summary>
        public DateTime Modified { get; private set; }


        internal bool TryGetCorrelationProperty(out CorrelationPropertyInfo sagaCorrelationProperty)
        {
            sagaCorrelationProperty = correlationProperty;


            return sagaCorrelationProperty != null;
        }

        /// <summary>
        /// Provides a way to update the actual saga entity.
        /// </summary>
        /// <param name="sagaEntity">The new entity.</param>
        public void AttachNewEntity(IContainSagaData sagaEntity)
        {
            Guard.AgainstNull(nameof(sagaEntity), sagaEntity);
            IsNew = true;
            AttachEntity(sagaEntity);
        }

        internal void AttachExistingEntity(IContainSagaData loadedEntity)
        {
            AttachEntity(loadedEntity);
        }

        void AttachEntity(IContainSagaData sagaEntity)
        {
            sagaId = sagaEntity.Id;
            UpdateModified();
            Instance.Entity = sagaEntity;
            SagaId = sagaEntity.Id.ToString();

            var properties = sagaEntity.GetType().GetProperties();
            SagaMetadata.CorrelationPropertyMetadata correlatedPropertyMetadata;

            if (Metadata.TryGetCorrelationProperty(out correlatedPropertyMetadata))
            {
                var propertyInfo = properties.Single(p => p.Name == correlatedPropertyMetadata.Name);
                var propertyValue = propertyInfo.GetValue(sagaEntity);

                var defaultValue = GetDefault(propertyInfo.PropertyType);

                var hasValue = propertyValue != null && !propertyValue.Equals(defaultValue);

                correlationProperty = new CorrelationPropertyInfo
                {
                    PropertyInfo = propertyInfo,
                    Value = propertyValue,
                    HasExistingValue = hasValue
                };
            }
        }

        void UpdateModified()
        {
            Modified = currentUtcDateTimeProvider();
        }

        internal void MarkAsNotFound()
        {
            NotFound = true;
            UpdateModified();
        }

        internal void Completed()
        {
            UpdateModified();
        }

        internal void Updated()
        {
            UpdateModified();
        }

        internal void ValidateChanges()
        {
            ValidateSagaIdIsNotModified();

            if (IsNew)
            {
                ValidateCorrelationPropertyHaveValue();
            }

            ValidateCorrelationPropertyNotModified();
        }

        void ValidateCorrelationPropertyHaveValue()
        {
            if (correlationProperty == null)
            {
                return;
            }

            var defaultValue = GetDefault(correlationProperty.PropertyInfo.PropertyType);

            if (!correlationProperty.Value.Equals(defaultValue))
            {
                return;
            }

            throw new Exception(
                $@"The correlated property '{correlationProperty.PropertyInfo.Name}' on saga '{Metadata.SagaType.Name}' does not have a value.
A correlated property must have a non default (i.e. non null and non-empty) value assigned when a new saga instance is created.");
        }

        void ValidateCorrelationPropertyNotModified()
        {
            if (correlationProperty == null)
            {
                return;
            }

            if (!correlationProperty.HasExistingValue)
            {
                return;
            }

            var currentValue = correlationProperty.PropertyInfo.GetValue(Instance.Entity);

            if (correlationProperty.Value.ToString() == currentValue.ToString())
            {
                return;
            }

            throw new Exception(
                $@"The value of the correlated property '{correlationProperty.PropertyInfo.Name}' on saga '{Metadata.SagaType.Name}' has changed from '{correlationProperty.Value}' to '{currentValue}'.
Changing the value of correlated properties at runtime is currently not supported.");
        }

        void ValidateSagaIdIsNotModified()
        {
            if (sagaId != Instance.Entity.Id)
            {
                throw new Exception("A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType: " + Metadata.SagaType.FullName);
            }
        }

        static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        CorrelationPropertyInfo correlationProperty;
        Guid sagaId;

        internal class CorrelationPropertyInfo
        {
            public bool HasExistingValue;
            public PropertyInfo PropertyInfo;
            public object Value;
        }
    }
}