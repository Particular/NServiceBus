namespace NServiceBus;

using System;
using System.Reflection;
using Configuration.AdvancedExtensibility;

/// <summary>
/// A set of extension methods for configuring unobtrusive DataBus properties.
/// </summary>
static class ConventionsBuilderExtensions
{
    /// <summary>
    /// Sets the function to be used to evaluate whether a property should be sent via the DataBus or not.
    /// </summary>
    [ObsoleteEx(
    Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
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