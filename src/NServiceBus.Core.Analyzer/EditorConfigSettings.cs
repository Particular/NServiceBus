#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

static class EditorConfigSettings
{
    /// <summary>
    /// Get an editorconfig setting in an Analyzer. The AnalyzerOptions should be accessible from an Analyzer through <c>context.Options</c>
    /// or in a CodeFixProvider from <c>context.Document.Project.AnalyzerOptions</c>.
    /// </summary>
    /// <returns></returns>
    public static bool TryGetValue(AnalyzerOptions analyzerOptions, SyntaxTree? syntaxTree, string settingName, [NotNullWhen(true)] out string? value)
    {
        var provider = analyzerOptions.AnalyzerConfigOptionsProvider;

        if (syntaxTree is null)
        {
            return provider.GlobalOptions.TryGetValue(settingName, out value);
        }

        var options = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return options.TryGetValue(settingName, out value);
    }

    public static bool KeyMatches(AnalyzerOptions analyzerOptions, SyntaxTree? syntaxTree, string settingName, string matchValue, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        => TryGetValue(analyzerOptions, syntaxTree, settingName, out var value) && string.Equals(value, matchValue, comparison);

    public const string HandlerStyleKey = "nservicebus_handler_style";
    public const string HandlerStyleInterfaces = "interfaces";
    public const string HandlerStyleConventions = "conventions";
}