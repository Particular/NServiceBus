#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;

sealed class KeyedServiceKey
{
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

    public object BaseKey { get; }
    object? ServiceKey { get; }

    public override bool Equals(object? obj)
    {
        if (obj is KeyedServiceKey other)
        {
            return Equals(BaseKey, other.BaseKey) && Equals(ServiceKey, other.ServiceKey);
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(BaseKey, ServiceKey);

    public override string? ToString() => ServiceKey == null ? BaseKey.ToString() : $"({BaseKey}, {ServiceKey})";
}