namespace NServiceBus.Core.Analyzer.Tests;

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NServiceBus;
using UniformSession;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[SetUpFixture]
public class SetUpFixture
{
    static SetUpFixture() => ProjectReferences =
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

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        AnalyzerTest.ConfigureAllAnalyzerTests(test => test.AddReferences(ProjectReferences));
        SourceGeneratorTest.ConfigureAllSourceGeneratorTests(test => test.WithInterceptorNamespace("NServiceBus"));
    }
}