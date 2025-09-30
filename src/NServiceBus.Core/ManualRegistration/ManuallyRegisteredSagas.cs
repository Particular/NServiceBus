namespace NServiceBus;

using System;
using System.Collections.Generic;

/// <summary>
/// Contains manually registered saga types.
/// </summary>
class ManuallyRegisteredSagas
{
    public List<SagaRegistration> Sagas { get; } = [];
}

/// <summary>
/// Represents a manually registered saga with its associated data type.
/// </summary>
/// <param name="SagaType">The saga type.</param>
/// <param name="SagaDataType">The saga data type.</param>
record struct SagaRegistration(Type SagaType, Type SagaDataType);

