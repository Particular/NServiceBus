#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;

/// <summary>
/// The storage types used for NServiceBus needs.
/// </summary>
public abstract class StorageType
{
    StorageType(string storage) => this.storage = storage;

    /// <inheritdoc />
    public override string ToString() => storage;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is StorageType other)
        {
            return storage == other.storage;
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode() => storage.GetHashCode();

    internal static IReadOnlyCollection<StorageType> GetAvailableStorageTypes() =>
        [Subscriptions.Instance, Sagas.Instance, Outbox.Instance];

    internal static StorageType Get<TStorage>() where TStorage : StorageType => typeof(TStorage) switch
    {
        { } t when t == typeof(Subscriptions) => Subscriptions.Instance,
        { } t when t == typeof(Sagas) => Sagas.Instance,
        { } t when t == typeof(Outbox) => Outbox.Instance,
        _ => throw new InvalidOperationException($"The storage type '{typeof(TStorage)}' is not supported.")
    };

    readonly string storage;

    /// <summary>
    /// Storage for subscriptions.
    /// </summary>
    public sealed class Subscriptions : StorageType
    {
        Subscriptions() : base("Subscriptions")
        {
        }

        internal static readonly StorageType Instance = new Subscriptions();
    }

    /// <summary>
    /// Storage for sagas.
    /// </summary>
    public sealed class Sagas : StorageType
    {
        Sagas() : base("Sagas")
        {
        }

        internal static readonly StorageType Instance = new Sagas();
    }

    /// <summary>
    /// Storage for outbox.
    /// </summary>
    public sealed class Outbox : StorageType
    {
        Outbox() : base("Outbox")
        {
        }

        internal static readonly StorageType Instance = new Outbox();
    }
}