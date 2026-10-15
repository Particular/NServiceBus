namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a saga instance being processed on the pipeline.
/// </summary>
public class ActiveSagaInstance
{
    readonly Func<DateTimeOffset> currentDateTimeOffsetProvider;

    /// <summary>
    /// Creates a new <see cref="ActiveSagaInstance"/> instance.
    /// </summary>
    public ActiveSagaInstance(Saga saga, SagaMetadata metadata, Func<DateTimeOffset> currentDateTimeOffsetProvider)
    {
        this.currentDateTimeOffsetProvider = currentDateTimeOffsetProvider;
        Instance = saga;
        Metadata = metadata;

        Created = currentDateTimeOffsetProvider();
        Modified = Created;
    }

    /// <summary>
    /// The id of the saga.
    /// </summary>
    public string SagaId { get; internal set; }

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
    public DateTimeOffset Created { get; }

    /// <summary>
    /// UTC timestamp of when the active saga instance was last modified.
    /// </summary>
    public DateTimeOffset Modified { get; private set; }

    /// <summary>
    /// Provides a way to update the actual saga entity.
    /// </summary>
    /// <param name="sagaEntity">The new entity.</param>
    public void AttachNewEntity(IContainSagaData sagaEntity)
    {
        ArgumentNullException.ThrowIfNull(sagaEntity);
        IsNew = true;
        AttachEntity(sagaEntity);
    }

    internal void AttachExistingEntity(IContainSagaData loadedEntity) => AttachEntity(loadedEntity);

    void AttachEntity(IContainSagaData sagaEntity)
    {
        sagaId = sagaEntity.Id;
        UpdateModified();
        Instance.Entity = sagaEntity;
        SagaId = sagaEntity.Id.ToString();

        if (!Metadata.TryGetCorrelationProperty(out var correlatedPropertyMetadata))
        {
            return;
        }

        var propertyValue = correlatedPropertyMetadata.Accessor.AccessFrom(sagaEntity);
        var defaultValue = SagaMapper.CorrelationPropertyTypeDefaultValues.GetValueOrDefault(correlatedPropertyMetadata.Type);
        var hasValue = propertyValue is not null && !propertyValue.Equals(defaultValue);
        correlationProperty = new CorrelationPropertyInfo(correlatedPropertyMetadata.Name, correlatedPropertyMetadata.Type, propertyValue, hasValue);
    }

    void UpdateModified() => Modified = currentDateTimeOffsetProvider();

    internal void MarkAsNotFound()
    {
        NotFound = true;
        UpdateModified();
    }

    internal void Completed() => UpdateModified();

    internal void Updated() => UpdateModified();

    internal void ValidateChanges()
    {
        ValidateSagaIdIsNotModified();

        if (!Metadata.TryGetCorrelationProperty(out var correlatedPropertyMetadata))
        {
            return;
        }

        var currentCorrelationPropertyValue = correlatedPropertyMetadata.Accessor.AccessFrom(Instance.Entity);

        if (IsNew)
        {
            ValidateCorrelationPropertyHaveValue(currentCorrelationPropertyValue);
        }

        ValidateCorrelationPropertyNotModified(currentCorrelationPropertyValue);
    }

    void ValidateCorrelationPropertyHaveValue(object currentCorrelationPropertyValue)
    {
        if (currentCorrelationPropertyValue != null)
        {
            return;
        }

        throw new Exception(
            $@"The correlated property '{correlationProperty.Name}' on saga '{Metadata.SagaType.Name}' does not have a value.
A correlated property must have a non-null value assigned when a new saga instance is created.");
    }

    void ValidateCorrelationPropertyNotModified(object currentCorrelationPropertyValue)
    {
        if (!correlationProperty.HasInitialValue)
        {
            return;
        }

        if (correlationProperty.InitialValue.Equals(currentCorrelationPropertyValue))
        {
            return;
        }

        throw new Exception(
            $@"The value of the correlated property '{correlationProperty.Name}' on saga '{Metadata.SagaType.Name}' has changed from '{correlationProperty.InitialValue}' to '{currentCorrelationPropertyValue}'.
Changing the value of correlated properties at runtime is currently not supported.");
    }

    void ValidateSagaIdIsNotModified()
    {
        if (sagaId != Instance.Entity.Id)
        {
            throw new Exception("A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType: " + Metadata.SagaType.FullName);
        }
    }

    CorrelationPropertyInfo correlationProperty;
    Guid sagaId;

    record CorrelationPropertyInfo(string Name, Type Type, object InitialValue, bool HasInitialValue);
}