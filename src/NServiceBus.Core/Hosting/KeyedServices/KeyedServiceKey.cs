#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Represents a composite key used for resolving services in a keyed service collection,
/// combining a base key with an optional service-specific key.
/// </summary>
public sealed class KeyedServiceKey
{
    /// <summary>
    /// Represents a composite key used for resolving services in a keyed service collection.
    /// Combines a base key with an optional service-specific key.
    /// </summary>
    public KeyedServiceKey(object baseKey, object? serviceKey = null)
    {
        if (baseKey is KeyedServiceKey key)
        {
            BaseKey = key.BaseKey;
            ServiceKey = key.ServiceKey;

            if (serviceKey is not null)
            {
                ServiceKey = serviceKey;
            }
        }
        else
        {
            BaseKey = baseKey;
            ServiceKey = serviceKey;
        }
    }

    /// <summary>
    /// Gets the base key component of the composite key, which is used to identify a service
    /// in a keyed service collection. This value is mandatory and serves as the primary
    /// identifier in the composite key structure.
    /// </summary>
    public object BaseKey { get; }

    /// <summary>
    /// Gets the service-specific key component of the composite key, which is optional and used to
    /// further differentiate services within the same base key in a keyed service collection.
    /// </summary>
    public object? ServiceKey { get; }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance of the KeyedServiceKey.
    /// </summary>
    /// <param name="obj">The object to compare with the current KeyedServiceKey, or <c>null</c>.</param>
    /// <returns>
    /// <c>true</c> if the specified object is equal to the current KeyedServiceKey; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is KeyedServiceKey other)
        {
            return Equals(BaseKey, other.BaseKey) && Equals(ServiceKey, other.ServiceKey);
        }
        return Equals(BaseKey, obj);
    }

    /// <summary>
    /// Returns a hash code for the current instance of the KeyedServiceKey.
    /// Combines the hash code of the base key and, if present, the service-specific key.
    /// </summary>
    /// <returns>
    /// An integer representing the hash code of the current KeyedServiceKey instance.
    /// </returns>
    public override int GetHashCode() => ServiceKey == null ? BaseKey.GetHashCode() : HashCode.Combine(BaseKey, ServiceKey);

    /// <summary>
    /// Returns a string representation of the current KeyedServiceKey instance.
    /// If the service-specific key is not present, returns the string representation
    /// of the base key. Otherwise, returns a composite string representation of both
    /// the base key and the service-specific key.
    /// </summary>
    /// <returns>
    /// A string representation of the current instance, including both the base key
    /// and the service-specific key, if present.
    /// </returns>
    public override string? ToString() => ServiceKey == null ? BaseKey.ToString() : $"({BaseKey}, {ServiceKey})";

    /// <summary>
    /// Creates a new instance of the <see cref="KeyedServiceKey"/> with the specified base key
    /// and a predefined value indicating a wildcard key.
    /// </summary>
    /// <param name="baseKey">The base key to use for the composite service key.</param>
    /// <returns>A <see cref="KeyedServiceKey"/> representing the wildcard configuration with the provided base key.</returns>
    public static KeyedServiceKey AnyKey(object baseKey) => new(baseKey, Any);

    /// <summary>
    /// Represents a constant wildcard value used in <see cref="KeyedServiceKey"/> to signify a match against
    /// any service-specific key within the keyed service collection.
    /// </summary>
    public const string Any = "_______<ANY>_______";
}