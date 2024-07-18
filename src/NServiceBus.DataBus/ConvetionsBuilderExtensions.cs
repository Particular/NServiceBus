namespace NServiceBus.DataBus;

using System.Reflection;

public static class ConvetionsBuilderExtensions
{
    /// <summary>
    /// Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
    /// </summary>
    public static ConventionsBuilder DefiningDataBusPropertiesAs(this ConventionsBuilder builder, Func<PropertyInfo, bool> definesDataBusProperty)
    {
        ArgumentNullException.ThrowIfNull(definesDataBusProperty);
        // Conventions.IsDataBusPropertyAction = definesDataBusProperty;
        return builder;
    }
}