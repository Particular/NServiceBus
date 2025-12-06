#nullable enable

namespace NServiceBus;

using System;
using Transport;

class SatelliteDefinition(
    string name,
    QueueAddress receiveAddress,
    PushRuntimeSettings runtimeSettings,
    Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy,
    OnSatelliteMessage onMessage)
{
    public string Name { get; } = name;

    public QueueAddress ReceiveAddress { get; } = receiveAddress;

    public PushRuntimeSettings RuntimeSettings { get; } = runtimeSettings;

    public Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> RecoverabilityPolicy { get; } = recoverabilityPolicy;

    public OnSatelliteMessage OnMessage { get; } = onMessage;
}