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
    public Task DoesNotReportOnEmpty()
    {
        var source = """
                     using NServiceBus;

                     [HandlerRegistryExtensions]
                     public static partial class FirstHandlerRegistryExtensions
                     {
                     }
                     """;

        return Assert(source);
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
    [TestCase("Not A Valid Identifier")]
    [TestCase("class")]
    [TestCase("++invalid")]
    [TestCase("event")]
    [TestCase("$InvalidStart")]
    public Task ReportsWhenEntryPointNameIsInvalid(string notValid)
    {
        var source = $$"""
                     using NServiceBus;

                     [HandlerRegistryExtensions([|"{{notValid}}"|])]
                     public static partial class InvalidEntryPointHandlerRegistryExtensions
                     {
                     }
                     """;

        return Assert(DiagnosticIds.HandlerRegistryExtensionsEntryPointInvalid, source);
    }

    [Test]
    [TestCase("ValidIdentifier")]
    [TestCase("Valid_Identifier123")]
    [TestCase("validIdentifier")]
    public Task DoesNotReportOnValid(string valid)
    {
        var source = $$"""
                       using NServiceBus;

                       [HandlerRegistryExtensions([|"{{valid}}"|])]
                       public static partial class InvalidEntryPointHandlerRegistryExtensions
                       {
                       }
                       """;

        return Assert(source);
    }
}