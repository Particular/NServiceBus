namespace NServiceBus.Core.Utils.Reflection;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

[TestFixture]
public class TypeExtensionMethodsTests
{
    [Test]
    public void SerializationFriendlyNameTests()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(typeof(string).SerializationFriendlyName(), Is.EqualTo("String"));
            Assert.That(typeof(Dictionary<string, int>).SerializationFriendlyName(), Is.EqualTo("DictionaryOfStringAndInt32"));
            Assert.That(typeof(Dictionary<string, Tuple<int>>).SerializationFriendlyName(), Is.EqualTo("DictionaryOfStringAndTupleOfInt32"));
            Assert.That(typeof(KeyValuePair<string, Tuple<int>>).SerializationFriendlyName(), Is.EqualTo("NServiceBus.KeyValuePairOfStringAndTupleOfInt32"));
        }
    }

    [Test]
    public void Should_return_return_different_results_for_different_types()
    {
        // This test verifies whether the added cache doesn't break the execution if called successively for two different types
        var customTypeResult = typeof(Target).IsSystemType();
        var systemTypeResult = typeof(string).IsSystemType();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(systemTypeResult, Is.True, "Expected string to be a system type.");
            Assert.That(customTypeResult, Is.False, "Expected Target to be a custom type.");
        }
    }

    class Target;

    [Test]
    public void Should_return_false_for_type_in_SN_and_non_particular_assembly() => Assert.That(typeof(string).IsFromParticularAssembly(), Is.False);

    [Test]
    public void Should_return_true_for_type_from_particular_assembly() => Assert.That(typeof(TransportReceiveToPhysicalMessageConnector).IsFromParticularAssembly(), Is.True);

    [Test]
    public void Should_return_false_for_type_in_non_SN_and_non_particular_assembly() => Assert.That(GetNonSnFakeType().IsFromParticularAssembly(), Is.False);

    [Test]
    public void Should_return_false_for_SN_and_non_particular_assembly() => Assert.That(typeof(string).Assembly.IsParticularAssembly(), Is.False);

    [Test]
    public void Should_return_true_for_particular_assembly() => Assert.That(typeof(TransportReceiveToPhysicalMessageConnector).Assembly.IsParticularAssembly(), Is.True);

    [Test]
    public void Should_return_false_for_non_SN_and_non_particular_assembly() => Assert.That(GetNonSnFakeType().Assembly.IsParticularAssembly(), Is.False);

    static Type GetNonSnFakeType()
    {
        var assemblyName = new AssemblyName { Name = "myAssembly" };
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var newModule = assemblyBuilder.DefineDynamicModule("myModule");
        var myType = newModule.DefineType("myType", TypeAttributes.Public);
        return myType.CreateType();
    }
}