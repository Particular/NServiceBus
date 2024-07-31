namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// This class contains helper methods to extract and cache databus properties from messages.
/// </summary>
class DataBusConventions
{
    /// <summary>
    /// Returns true if the given property should be send via the DataBus.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public bool IsDataBusProperty(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        try
        {
            return IsDataBusPropertyAction(property);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to evaluate DataBus Property convention. See inner exception for details.", ex);
        }
    }

    internal List<DataBusPropertyInfo> GetDataBusProperties(object message)
    {
        return cache.GetOrAdd(message.GetType(), messageType =>
        {
            var properties = new List<DataBusPropertyInfo>();
            foreach (var propertyInfo in messageType.GetProperties())
            {
                if (IsDataBusProperty(propertyInfo))
                {
                    properties.Add(new DataBusPropertyInfo
                    {
                        Name = propertyInfo.Name,
                        Type = propertyInfo.PropertyType,
                        Getter = DelegateFactory.CreateGet(propertyInfo),
                        Setter = DelegateFactory.CreateSet(propertyInfo)
                    });
                }
            }

            return properties;
        });
    }

    internal void DefiningDataBusPropertiesAs(Func<PropertyInfo, bool> definesDataBusProperty)
    {
        IsDataBusPropertyAction = definesDataBusProperty;
    }

    internal Func<PropertyInfo, bool> IsDataBusPropertyAction = p => typeof(IDataBusProperty).IsAssignableFrom(p.PropertyType) && typeof(IDataBusProperty) != p.PropertyType;

    readonly ConcurrentDictionary<Type, List<DataBusPropertyInfo>> cache = new ConcurrentDictionary<Type, List<DataBusPropertyInfo>>();
}