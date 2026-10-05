#nullable enable

namespace NServiceBus.Core.Analyzer.Tests;

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

[TestFixture]
public class MessagingMigrationSyntaxTests
{
    // V11 needs syntax to distinguish explicit <object> from inferred object.
    static bool HasExplicitTypeArgumentList(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            GenericNameSyntax genericName => genericName.TypeArgumentList.Arguments.Count > 0,
            MemberAccessExpressionSyntax { Name: GenericNameSyntax name } => name.TypeArgumentList.Arguments.Count > 0,
            MemberBindingExpressionSyntax { Name: GenericNameSyntax name } => name.TypeArgumentList.Arguments.Count > 0,
            _ => false
        };

    [TestCase("session.Send<object>(message)", true)]
    [TestCase("session.Send(message)", false)]
    [TestCase("Send<object>(message)", true)]
    [TestCase("Send(message)", false)]
    [TestCase("session.Send<MyMessage>(message)", true)]
    [TestCase("session?.Send<object>(message)", true)]
    [TestCase("session?.Send(message)", false)]
    public void HasExplicitTypeArgumentList(string expression, bool expected)
    {
        var source = $"class C {{ void M() {{ {expression}; }} }}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var invocation = tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single();
        Assert.That(HasExplicitTypeArgumentList(invocation), Is.EqualTo(expected));
    }
}
