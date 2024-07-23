namespace NServiceBus.DataBus;

using System;
using System.Reflection;
using Configuration.AdvancedExtensibility;

public static class ConvetionsBuilderExtensions
{
    /// <summary>
    /// Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
    /// </summary>
    public static ConventionsBuilder DefiningDataBusPropertiesAs(this ConventionsBuilder builder, Func<PropertyInfo, bool> definesDataBusProperty)
    {
        ArgumentNullException.ThrowIfNull(definesDataBusProperty);

        var dataBusConventions = builder.GetSettings().GetOrDefault<DataBusConventions>(Features.DataBus.DataBusConventionsKey);

        if (dataBusConventions == null)
        {
            dataBusConventions = new DataBusConventions();
            builder.GetSettings().Set(Features.DataBus.DataBusConventionsKey, dataBusConventions);
        }

        dataBusConventions.IsDataBusPropertyAction = definesDataBusProperty;

        return builder;
    }
}