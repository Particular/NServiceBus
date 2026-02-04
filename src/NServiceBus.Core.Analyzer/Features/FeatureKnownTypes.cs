#nullable enable

namespace NServiceBus.Core.Analyzer.Features;

using Microsoft.CodeAnalysis;

public readonly record struct FeatureKnownTypes(
    INamedTypeSymbol Feature,
    INamedTypeSymbol SettingsExtensions)
{
    public static bool TryGet(Compilation compilation, out FeatureKnownTypes knownTypes)
    {
        var feature = compilation.GetTypeByMetadataName("NServiceBus.Features.Feature");
        var settingsExtensions = compilation.GetTypeByMetadataName("NServiceBus.Features.SettingsExtensions");

        if (feature is null || settingsExtensions is null)
        {
            knownTypes = default;
            return false;
        }

        knownTypes = new FeatureKnownTypes(feature, settingsExtensions);
        return true;
    }
}