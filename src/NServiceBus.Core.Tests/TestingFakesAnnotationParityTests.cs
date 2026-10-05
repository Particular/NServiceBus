namespace NServiceBus.Core.Tests;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NServiceBus.Pipeline;
using NServiceBus.Testing;
using NUnit.Framework;

[TestFixture]
public class TestingFakesAnnotationParityTests
{
    [TestCase(typeof(TestablePipelineContext), typeof(IPipelineContext), "Send")]
    [TestCase(typeof(TestablePipelineContext), typeof(IPipelineContext), "Publish")]
    [TestCase(typeof(TestableMessageSession), typeof(IMessageSession), "Send")]
    [TestCase(typeof(TestableMessageSession), typeof(IMessageSession), "Publish")]
    [TestCase(typeof(TestableMessageProcessingContext), typeof(IMessageProcessingContext), "Reply")]
    [TestCase(typeof(TestableOutgoingLogicalMessageContext), typeof(IOutgoingLogicalMessageContext), "UpdateMessage")]
    public void Object_only_method_matches_interface_RequiresUnreferencedCode(Type fakeType, Type interfaceType, string methodName)
    {
        var fake = FindObjectOnly(fakeType, methodName);
        var iface = FindObjectOnly(interfaceType, methodName);

        Assert.That(fake, Is.Not.Null, $"{fakeType.Name}.{methodName} object-only overload not found");
        Assert.That(iface, Is.Not.Null, $"{interfaceType.Name}.{methodName} object-only overload not found");

        Assert.That(HasAttribute<RequiresUnreferencedCodeAttribute>(fake),
            Is.EqualTo(HasAttribute<RequiresUnreferencedCodeAttribute>(iface)),
            $"RequiresUnreferencedCode annotation mismatch on {fakeType.Name}.{methodName}(object)");
    }

    [TestCase(typeof(TestablePipelineContext), typeof(IPipelineContext), "Send")]
    [TestCase(typeof(TestablePipelineContext), typeof(IPipelineContext), "Publish")]
    [TestCase(typeof(TestableMessageSession), typeof(IMessageSession), "Send")]
    [TestCase(typeof(TestableMessageSession), typeof(IMessageSession), "Publish")]
    [TestCase(typeof(TestableMessageProcessingContext), typeof(IMessageProcessingContext), "Reply")]
    [TestCase(typeof(TestableOutgoingLogicalMessageContext), typeof(IOutgoingLogicalMessageContext), "UpdateMessage")]
    public void Typed_method_matches_interface_annotations(Type fakeType, Type interfaceType, string methodName)
    {
        var fake = FindTyped(fakeType, methodName);
        var iface = FindTyped(interfaceType, methodName);

        Assert.That(fake, Is.Not.Null, $"{fakeType.Name}.{methodName}<T> typed overload not found");
        Assert.That(iface, Is.Not.Null, $"{interfaceType.Name}.{methodName}<T> typed overload not found");

        Assert.That(GetOverloadResolutionPriority(fake), Is.EqualTo(GetOverloadResolutionPriority(iface)),
            $"OverloadResolutionPriority mismatch on {fakeType.Name}.{methodName}<T>");

        Assert.That(HasDynamicallyAccessedMembersOnGenericArgument(fake),
            Is.EqualTo(HasDynamicallyAccessedMembersOnGenericArgument(iface)),
            $"DynamicallyAccessedMembers on T mismatch on {fakeType.Name}.{methodName}<T>");
    }

    [TestCase(typeof(TestablePipelineContext), typeof(IPipelineContext), "Send")]
    [TestCase(typeof(TestablePipelineContext), typeof(IPipelineContext), "Publish")]
    [TestCase(typeof(TestableMessageSession), typeof(IMessageSession), "Send")]
    [TestCase(typeof(TestableMessageSession), typeof(IMessageSession), "Publish")]
    [TestCase(typeof(TestableMessageProcessingContext), typeof(IMessageProcessingContext), "Reply")]
    [TestCase(typeof(TestableOutgoingLogicalMessageContext), typeof(IOutgoingLogicalMessageContext), "UpdateMessage")]
    public void Explicit_type_method_matches_interface_DynamicallyAccessedMembers(Type fakeType, Type interfaceType, string methodName)
    {
        var fake = FindExplicitType(fakeType, methodName);
        var iface = FindExplicitType(interfaceType, methodName);

        Assert.That(fake, Is.Not.Null, $"{fakeType.Name}.{methodName}(object, Type) explicit-type overload not found");
        Assert.That(iface, Is.Not.Null, $"{interfaceType.Name}.{methodName}(object, Type) explicit-type overload not found");

        Assert.That(HasDynamicallyAccessedMembersOnTypeParameter(fake),
            Is.EqualTo(HasDynamicallyAccessedMembersOnTypeParameter(iface)),
            $"DynamicallyAccessedMembers on messageType mismatch on {fakeType.Name}.{methodName}(object, Type)");
    }

    static MethodInfo FindObjectOnly(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(m => m.Name == name
                && !m.IsGenericMethodDefinition
                && m.GetParameters().Length > 0
                && m.GetParameters()[0].ParameterType == typeof(object)
                && !m.GetParameters().Any(p => p.ParameterType == typeof(Type)));

    static MethodInfo FindTyped(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(m => m.Name == name
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length > 0
                && m.GetParameters()[0].ParameterType.IsGenericParameter);

    static MethodInfo FindExplicitType(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(m => m.Name == name
                && !m.IsGenericMethodDefinition
                && m.GetParameters().Length > 1
                && m.GetParameters()[0].ParameterType == typeof(object)
                && m.GetParameters().Any(p => p.ParameterType == typeof(Type)));

    static bool HasAttribute<T>(MethodInfo method) where T : Attribute =>
        method.GetCustomAttributes<T>().Any();

    static int? GetOverloadResolutionPriority(MethodInfo method) =>
        method.GetCustomAttributes<OverloadResolutionPriorityAttribute>()
            .Select(a => (int?)a.Priority)
            .FirstOrDefault();

    static bool HasDynamicallyAccessedMembersOnGenericArgument(MethodInfo method) =>
        method.IsGenericMethodDefinition
        && method.GetGenericArguments()[0].GetCustomAttributes<DynamicallyAccessedMembersAttribute>().Any();

    static bool HasDynamicallyAccessedMembersOnTypeParameter(MethodInfo method) =>
        method.GetParameters().First(p => p.ParameterType == typeof(Type))
            .GetCustomAttributes<DynamicallyAccessedMembersAttribute>().Any();
}
