namespace NServiceBus.AcceptanceTesting.Support;

using System;

public sealed class KeyedServiceKey
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

    public object? ServiceKey { get; }

    public override bool Equals(object? obj)
    {
        if (obj is KeyedServiceKey other)
        {
            return Equals(BaseKey, other.BaseKey) && Equals(ServiceKey, other.ServiceKey);
        }
        return Equals(BaseKey, obj);
    }

    public override int GetHashCode() => ServiceKey == null ? BaseKey.GetHashCode() : HashCode.Combine(BaseKey, ServiceKey);

    public override string? ToString() => ServiceKey == null ? BaseKey.ToString() : $"({BaseKey}, {ServiceKey})";

    public static KeyedServiceKey AnyKey(object baseKey) => new(baseKey, Any);

    public const string Any = "_______<ANY>_______";
}