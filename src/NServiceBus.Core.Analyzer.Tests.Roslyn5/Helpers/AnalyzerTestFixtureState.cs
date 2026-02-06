namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using UniformSession;

static class AnalyzerTestFixtureState
{
    internal static readonly bool VerboseLogging = Environment.GetEnvironmentVariable("CI") != "true" || Environment.GetEnvironmentVariable("VERBOSE_TEST_LOGGING")?.ToLower() == "true";

    static AnalyzerTestFixtureState() => ProjectReferences =
    [
        MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location),
        MetadataReference.CreateFromFile(typeof(EndpointConfiguration).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IUniformSession).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IMessage).GetTypeInfo().Assembly.Location)
    ];

    internal static readonly ImmutableList<PortableExecutableReference> ProjectReferences;

    internal static readonly string[] OpeningSeparator = ["[|"];
    internal static readonly string[] ClosingSeparator = ["|]"];
}