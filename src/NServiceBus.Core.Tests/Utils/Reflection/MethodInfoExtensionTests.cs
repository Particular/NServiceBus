namespace NServiceBus.Core.Utils.Reflection;

using System.Reflection;
using NUnit.Framework;

[TestFixture]
public class MethodInfoExtensionTests
{
    [Test]
    public void When_method_has_no_return_type_Should_return_null()
    {
        var instance = new TypeWithMethods();
        var result = TypeWithMethods.MethodWithNoReturnInfo.InvokeGeneric(instance, [typeof(InputType)]);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void When_method_has_return_type_Should_return_value()
    {
        var instance = new TypeWithMethods();
        var result = TypeWithMethods.MethodWithReturnInfo.InvokeGeneric(instance, [typeof(InputType)]);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void When_method_has_return_type_Should_return_strong_typed_value()
    {
        var instance = new TypeWithMethods();
        var result = TypeWithMethods.MethodWithReturnInfo.InvokeGeneric<int>(instance, [], [typeof(InputType)]);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void When_static_method_has_no_return_type_Should_return_null()
    {
        var result = TypeWithMethods.StaticMethodWithNoReturnInfo.InvokeGeneric(null, [], [typeof(InputType)]);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void When_static_method_has_return_type_Should_return_value()
    {
        var result = TypeWithMethods.StaticMethodWithReturnInfo.InvokeGeneric(null, [typeof(InputType)]);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void When_static_method_has_return_type_Should_return_strong_typed_value()
    {
        var result = TypeWithMethods.StaticMethodWithReturnInfo.InvokeGeneric<int>(null, [], [typeof(InputType)]);

        Assert.That(result, Is.EqualTo(42));
    }

    class InputType;

    class TypeWithMethods
    {
#pragma warning disable CA1822
        public void MethodWithNoReturn<T>() { }
        public int MethodWithReturn<T>() => 42;
#pragma warning restore CA1822

        public static void StaticMethodWithNoReturn<T>() { }

        public static int StaticMethodWithReturn<T>() => 42;

        public static MethodInfo StaticMethodWithNoReturnInfo = typeof(TypeWithMethods).GetMethod("StaticMethodWithNoReturn", BindingFlags.Public | BindingFlags.Static);
        public static MethodInfo MethodWithNoReturnInfo = typeof(TypeWithMethods).GetMethod("MethodWithNoReturn", BindingFlags.Public | BindingFlags.Instance);
        public static MethodInfo MethodWithReturnInfo = typeof(TypeWithMethods).GetMethod("MethodWithReturn", BindingFlags.Public | BindingFlags.Instance);
        public static MethodInfo StaticMethodWithReturnInfo = typeof(TypeWithMethods).GetMethod("StaticMethodWithReturn", BindingFlags.Public | BindingFlags.Static);
    }
}