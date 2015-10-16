namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Represents a saga instance being processed on the pipeline.
    /// </summary>
    public class ActiveSagaInstance
    {
        internal ActiveSagaInstance(Saga saga, SagaMetadata metadata)
        {
            Instance = saga;
            Metadata = metadata;
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
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "context.MessageHandler.Instance")]
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
        /// Provides a way to update the actual saga entity.
        /// </summary>
        /// <param name="sagaEntity">The new entity.</param>
        public void AttachNewEntity(IContainSagaData sagaEntity)
        {
            Guard.AgainstNull("sagaEntity", sagaEntity);
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
            Instance.Entity = sagaEntity;
            SagaId = sagaEntity.Id.ToString();

            var properties = sagaEntity.GetType().GetProperties();

            foreach (var correlatedProperty in Metadata.CorrelationProperties)
            {
                var propertyInfo = properties.Single(p => p.Name == correlatedProperty.Name);
                var propertyValue = propertyInfo.GetValue(sagaEntity);

                var defaultValue = GetDefault(propertyInfo.PropertyType);

                var hasValue = propertyValue != null && !propertyValue.Equals(defaultValue);

                correlationProperties.Add(new CorrelationProperty
                {
                    PropertyInfo = propertyInfo,
                    Value = propertyValue,
                    HasExistingValue = hasValue
                });
            }

        }

        internal void MarkAsNotFound()
        {
            NotFound = true;
        }

   
        internal void ValidateChanges()
        {
            ValidateSagaIdIsNotModified();

            if (IsNew)
            {
                ValidateAllCorrelationPropertiesHaveValues();
            }

            ValidateCorrelationPropertiesNotModified();
        }

        void ValidateAllCorrelationPropertiesHaveValues()
        {
              foreach (var correlationProperty in correlationProperties)
            {
                var defaultValue = GetDefault(correlationProperty.PropertyInfo.PropertyType);

                if (correlationProperty.Value.Equals(defaultValue))
                {
                    throw new Exception(
                        $@"We detected that the correlated property '{correlationProperty.PropertyInfo.Name}' on saga '{Metadata.SagaType.Name}' does not have a value'. 
All correlated properties must have a non null or empty value assigned to them when a new saga instance is created.");
                }
            }
        }

        void ValidateCorrelationPropertiesNotModified()
        {
            foreach (var correlationProperty in correlationProperties.Where(cp=>cp.HasExistingValue))
            {
                var currentValue = correlationProperty.PropertyInfo.GetValue(Instance.Entity);

                if (correlationProperty.Value.ToString() != currentValue.ToString())
                {
                    throw new Exception(
                        $@"We detected that the value of the correlated property '{correlationProperty.PropertyInfo.Name}' on saga '{Metadata.SagaType.Name}' has changed from '{correlationProperty.Value}' to '{currentValue}'. 
Changing the value of correlated properties at runtime is currently not supported.");
                }
            }
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

        internal IDictionary<string, object> CorrelationProperties
        {
            get { return correlationProperties.ToDictionary(cp => cp.PropertyInfo.Name, cp => cp.Value); }
        }
        List<CorrelationProperty> correlationProperties = new List<CorrelationProperty>();
        Guid sagaId;

        class CorrelationProperty
        {
            public PropertyInfo PropertyInfo;
            public object Value;
            public bool HasExistingValue;
        }
    }
}