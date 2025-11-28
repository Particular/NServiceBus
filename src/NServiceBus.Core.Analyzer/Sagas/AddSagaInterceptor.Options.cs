#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed partial class AddSagaInterceptor
{
    internal readonly record struct Options(bool GenerateUnsafeAccessors)
    {
        public static Options Create(AnalyzerConfigOptionsProvider optionsProvider)
        {
            var disableUnsafeAccessors = TryGetBooleanMsBuildProperty(optionsProvider.GlobalOptions, DisableSagaUnsafeAccessorsPropertyName);

            return new Options(!disableUnsafeAccessors);
        }

        static bool TryGetBooleanMsBuildProperty(AnalyzerConfigOptions options, string propertyName) =>
            options.TryGetValue($"build_property.{propertyName}", out var value) &&
            value.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);

        const string DisableSagaUnsafeAccessorsPropertyName = "NServiceBusDisableSagaUnsafeAccessors";
    }
}