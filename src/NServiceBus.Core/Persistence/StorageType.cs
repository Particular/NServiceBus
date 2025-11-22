#nullable enable

namespace NServiceBus;

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

    internal virtual Options Defaults { get; } = new Options();

    /// <summary>
    /// Checks whether the storage type supports the given options.
    /// </summary>
    /// <param name="options">The options to verify.</param>
    /// <returns>Returns true if the storage type supports the given options. Otherwise false.</returns>
    protected internal virtual bool Supports(Options options) => false;

    internal static IReadOnlyCollection<StorageType> GetAvailableStorageTypes() =>
        [new Subscriptions(), new Sagas(), new Outbox()];

    readonly string storage;

    /// <summary>
    /// Options for storage types.
    /// </summary>
    public record Options;

    /// <summary>
    /// Storage for subscriptions.
    /// </summary>
    public sealed class Subscriptions : StorageType
    {
        /// <summary>
        /// Creates a new instance of the subscriptions storage type.
        /// </summary>
        public Subscriptions() : base("Subscriptions")
        {
        }

        internal static readonly StorageType Instance = new Subscriptions();
    }

    /// <summary>
    /// Storage for sagas.
    /// </summary>
    public sealed class Sagas : StorageType
    {
        /// <summary>
        /// Creates a new instance of the sagas storage type.
        /// </summary>
        public Sagas() : base("Sagas")
        {
        }

        /// <inheritdoc />.
        protected internal override bool Supports(Options options) => options is SagasOptions;

        internal override Options Defaults { get; } = new SagasOptions();

        internal static readonly StorageType Instance = new Sagas();
    }

    /// <summary>
    /// Options for sagas storage.
    /// </summary>
    public sealed record SagasOptions : Options
    {
        /// <summary>
        /// Indicates whether the storage supports finders.
        /// </summary>
        public bool SupportsFinders { get; init; } = false;
    }

    /// <summary>
    /// Storage for outbox.
    /// </summary>
    public sealed class Outbox : StorageType
    {
        /// <summary>
        /// Creates a new instance of the outbox storage type.
        /// </summary>
        public Outbox() : base("Outbox")
        {
        }

        internal static readonly StorageType Instance = new Outbox();
    }
}