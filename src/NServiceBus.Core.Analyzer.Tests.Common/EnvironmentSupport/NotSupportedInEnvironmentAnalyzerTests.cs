namespace NServiceBus.Core.Analyzer.Tests.EnvironmentSupport;

using System.Collections.Generic;
using System.Threading.Tasks;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class NotSupportedInEnvironmentAnalyzerTests : AnalyzerTestFixture<NotSupportedInEnvironmentAnalyzer>
{
    [Test]
    public Task NoEnvironmentProperty_NoDiagnostics()
    {
        var source =
            """
            using NServiceBus;
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Not supported in Azure Functions")]
                public static void ForbiddenMethod() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    TestApi.ForbiddenMethod();
                }
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task MethodInvocationFlagged()
    {
        var source =
            """
            using NServiceBus;
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Method not supported")]
                public static void ForbiddenMethod() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    [|TestApi.ForbiddenMethod()|];
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "AzureFunctionsIsolated" }
        };

        return Assert(DiagnosticIds.NotSupportedInEnvironment, source, options);
    }

    [Test]
    public Task TypeLevelAttribute_TriggersDiagnostic()
    {
        var source =
            """
            using NServiceBus;
            [NotSupportedInEnvironment("AzureFunctionsIsolated", "Type not supported")]
            public class ForbiddenType
            {
                public void Method() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    var instance = [|new ForbiddenType()|];
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "AzureFunctionsIsolated" }
        };

        return Assert(DiagnosticIds.NotSupportedInEnvironment, source, options);
    }

    [Test]
    public Task MethodLevelOverridesTypeLevelReason()
    {
        var source =
            """
            using NServiceBus;
            [NotSupportedInEnvironment("AzureFunctionsIsolated", "Type level reason")]
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Method level reason")]
                public static void ForbiddenMethod() { }
            }
            public class TestUsage
            {
                public void TestMethod()
                {
                    [|TestApi.ForbiddenMethod()|];
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "AzureFunctionsIsolated" }
        };

        return Assert(DiagnosticIds.NotSupportedInEnvironment, source, options);
    }

    [Test]
    public Task ObjectCreationFlagged()
    {
        var source =
            """
            using NServiceBus;
            public class ForbiddenClass
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Constructor not supported")]
                public ForbiddenClass() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    var instance = [|new ForbiddenClass()|];
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "AzureFunctionsIsolated" }
        };

        return Assert(DiagnosticIds.NotSupportedInEnvironment, source, options);
    }

    [Test]
    public Task PropertyReferenceFlagged()
    {
        var source =
            """
            using NServiceBus;
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Property not supported")]
                public static string ForbiddenProperty { get; set; }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    var value = [|TestApi.ForbiddenProperty|];
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "AzureFunctionsIsolated" }
        };

        return Assert(DiagnosticIds.NotSupportedInEnvironment, source, options);
    }

    [Test]
    public Task DifferentEnvironment_NoDiagnostic()
    {
        var source =
            """
            using NServiceBus;
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Not supported")]
                public static void ForbiddenMethod() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    TestApi.ForbiddenMethod();
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "SomeOtherEnvironment" }
        };

        return Assert(source, options);
    }

    [Test]
    public Task MultipleAttributes_MatchingEnvironmentTriggersDiagnostic()
    {
        var source =
            """
            using NServiceBus;
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Not in Azure Functions")]
                [NotSupportedInEnvironment("LambdaEnvironment", "Not in Lambda")]
                public static void ForbiddenMethod() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    [|TestApi.ForbiddenMethod()|];
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "AzureFunctionsIsolated" }
        };

        return Assert(DiagnosticIds.NotSupportedInEnvironment, source, options);
    }

    [Test]
    public Task MultipleAttributes_NonMatchingEnvironmentNoDiagnostic()
    {
        var source =
            """
            using NServiceBus;
            public class TestApi
            {
                [NotSupportedInEnvironment("AzureFunctionsIsolated", "Not in Azure Functions")]
                [NotSupportedInEnvironment("LambdaEnvironment", "Not in Lambda")]
                public static void ForbiddenMethod() { }
            }
            public class TestClass
            {
                public void TestMethod()
                {
                    TestApi.ForbiddenMethod();
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            { "build_property.NServiceBusEnvironment", "ContainerEnvironment" }
        };

        return Assert(source, options);
    }
}