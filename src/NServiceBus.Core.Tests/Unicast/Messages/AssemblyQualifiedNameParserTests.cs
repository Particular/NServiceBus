namespace NServiceBus.Unicast.Tests;

using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class AssemblyQualifiedNameParserTests
{
    [Theory]
    [TestCaseSource(nameof(AssemblyQualifiedNames))]
    public void Should_extract_fullname_from_qualified_assembly_name(string assemblyQualifiedName, string expectedFullName)
    {
        Console.WriteLine(assemblyQualifiedName);

        var typeNameWithoutAssembly = AssemblyQualifiedNameParser.GetMessageTypeNameWithoutAssembly(assemblyQualifiedName);

        Assert.That(typeNameWithoutAssembly.ToString(), Is.EqualTo(expectedFullName));
    }

    static IEnumerable<object[]> AssemblyQualifiedNames =>
    [
        [typeof(ICommand).AssemblyQualifiedName, typeof(ICommand).FullName],
        [typeof(MyEvent).AssemblyQualifiedName, typeof(MyEvent).FullName],
        // Array types
        [typeof(ICommand[]).AssemblyQualifiedName, typeof(ICommand[]).FullName],
        [typeof(MyEvent[]).AssemblyQualifiedName, typeof(MyEvent[]).FullName],
        // Yes generic are not officially supported but we should still make sure the type parsing logic doesn't break
        [typeof(MyEventGeneric<>).AssemblyQualifiedName, typeof(MyEventGeneric<>).FullName],
        [typeof(MyEventGeneric<MyEvent>).AssemblyQualifiedName, typeof(MyEventGeneric<MyEvent>).FullName],
        [typeof(MyEventGeneric<MyEventGeneric<MyEvent>>).AssemblyQualifiedName, typeof(MyEventGeneric<MyEventGeneric<MyEvent>>).FullName],
        // Generic array types
        [typeof(MyEventGeneric<MyEvent>[]).AssemblyQualifiedName, typeof(MyEventGeneric<MyEvent>[]).FullName],
        [typeof(MyEventGeneric<MyEventGeneric<MyEvent>>[]).AssemblyQualifiedName, typeof(MyEventGeneric<MyEventGeneric<MyEvent>>[]).FullName],
        // Generic Nested array types
        [typeof(MyEventGeneric<MyEventGeneric<MyEvent>[]>[]).AssemblyQualifiedName, typeof(MyEventGeneric<MyEventGeneric<MyEvent>[]>[]).FullName],
        // Special case according to https://learn.microsoft.com/de-de/dotnet/api/system.type.assemblyqualifiedname
        ["TopNamespace.Sub\\+Namespace.ContainingClass+NestedClass, MyAssembly, Version=1.3.0.0, Culture=neutral, PublicKeyToken=b17a5c561934e089", "TopNamespace.Sub\\+Namespace.ContainingClass+NestedClass"]
    ];

    [Theory]
    [TestCaseSource(nameof(FullNames))]
    public void Should_extract_fullname_from_fullname(string assemblyQualifiedName, string expectedFullName)
    {
        Console.WriteLine(assemblyQualifiedName);

        var messageType = AssemblyQualifiedNameParser.GetMessageTypeNameWithoutAssembly(assemblyQualifiedName);

        Assert.That(messageType.ToString(), Is.EqualTo(expectedFullName));
    }

    static IEnumerable<object[]> FullNames =>
    [
        [typeof(ICommand).FullName, typeof(ICommand).FullName],
        [typeof(MyEvent).FullName, typeof(MyEvent).FullName],
        // Array types
        [typeof(ICommand[]).FullName, typeof(ICommand[]).FullName],
        [typeof(MyEvent[]).FullName, typeof(MyEvent[]).FullName],
        // Yes generic are not officially supported but we should still make sure the type parsing logic doesn't break
        [typeof(MyEventGeneric<>).FullName, typeof(MyEventGeneric<>).FullName],
        [typeof(MyEventGeneric<MyEvent>).FullName, typeof(MyEventGeneric<MyEvent>).FullName],
        [typeof(MyEventGeneric<MyEventGeneric<MyEvent>>).FullName, typeof(MyEventGeneric<MyEventGeneric<MyEvent>>).FullName],
        // Generic array types
        [typeof(MyEventGeneric<MyEvent>[]).FullName, typeof(MyEventGeneric<MyEvent>[]).FullName],
        [typeof(MyEventGeneric<MyEventGeneric<MyEvent>>[]).FullName, typeof(MyEventGeneric<MyEventGeneric<MyEvent>>[]).FullName],
        // Generic Nested array types
        [typeof(MyEventGeneric<MyEventGeneric<MyEvent>[]>[]).FullName, typeof(MyEventGeneric<MyEventGeneric<MyEvent>[]>[]).FullName],
        // Special case according to https://learn.microsoft.com/de-de/dotnet/api/system.type.assemblyqualifiedname
        ["TopNamespace.Sub\\+Namespace.ContainingClass+NestedClass", "TopNamespace.Sub\\+Namespace.ContainingClass+NestedClass"]
    ];

    class MyEvent;

    class MyEventGeneric<T>;
}