namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Helpers;
using NUnit.Framework;

public class HandlerRegistryExtensionsAttributeAnalyzerTests : AnalyzerTestFixture<HandlerRegistryExtensionsAttributeAnalyzer>
{
    [Test]
    public Task ReportsMultipleDeclarations()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryExtensions]
                     public static partial class FirstHandlerRegistryExtensions
                     {
                     }

                     [[|HandlerRegistryExtensions|]]
                     public static partial class SecondHandlerRegistryExtensions
                     {
                     }
                     """;

        return Assert(DiagnosticIds.MultipleHandlerRegistryExtensions, source);
    }

    [Test]
    public Task ReportsWhenNotPartial()
    {
        var source = """
                     using NServiceBus;

                     [[|HandlerRegistryExtensions|]]
                     public static class MissingPartialHandlerRegistryExtensions
                     {
                     }
                     """;

        return Assert(DiagnosticIds.HandlerRegistryExtensionsMustBePartial, source);
    }

    [Test]
    public Task ReportsWhenEntryPointNameIsInvalid()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryExtensions([[|"Not A Valid Identifier"|]])]
                     public static partial class InvalidEntryPointHandlerRegistryExtensions
                     {
                     }
                     """;

        return Assert(DiagnosticIds.HandlerRegistryExtensionsEntryPointInvalid, source);
    }

    [Test]
    public Task ReportsWhenEntryPointNameIsKeyword()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryExtensions([[|"class"|]])]
                     public static partial class KeywordEntryPointHandlerRegistryExtensions
                     {
                     }
                     """;

        return Assert(DiagnosticIds.HandlerRegistryExtensionsEntryPointInvalid, source);
    }
}
