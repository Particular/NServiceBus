#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Utility;

[Generator(LanguageNames.CSharp)]
public sealed class InterceptionsLocationAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) =>
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            var sourceWriter = new SourceWriter()
                .PreAmble()
                .WithFileScopedNamespace("System.Runtime.CompilerServices")
                .WithGeneratedCodeAttribute();

            sourceWriter.WriteLine("""
                                   [global::System.Diagnostics.Conditional("DEBUG")]
                                   [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                                   internal sealed class InterceptsLocationAttribute : global::System.Attribute
                                   {
                                       public InterceptsLocationAttribute(int version, string data)
                                       {
                                           _ = version;
                                           _ = data;
                                       }
                                   }
                                   """);

            postInitializationContext.AddSource("InterceptionsLocationAttribute.g.cs", sourceWriter.ToSourceText());
        });
}
