namespace NServiceBus.Core.Tests;

using Extensibility;
using NUnit.Framework;

[TestFixture]
public class ContextBagTests
{
    [Test]
    public void Should_allow_storing_null_values()
    {
        var contextBag = new ContextBag();

        contextBag.Set<string>("NullValue", null);

        var result = ((IReadOnlyContextBag)contextBag).TryGet("NullValue", out object theValue);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should be able to retrieve a null value");
            Assert.That(theValue, Is.Null);
        });
    }

    [Test]
    public void ShouldAllowMonkeyPatching()
    {
        var contextBag = new ContextBag();

        contextBag.Set("MonkeyPatch", "some string");

        ((IReadOnlyContextBag)contextBag).TryGet("MonkeyPatch", out string theValue);
        Assert.That(theValue, Is.EqualTo("some string"));
    }

    [Test]
    public void SetOnRoot_should_set_value_on_root_context()
    {
        const string key = "testkey";

        var root = new ContextBag();
        var intermediate = new ContextBag(root);
        var context = new ContextBag(intermediate);
        var fork = new ContextBag(intermediate);

        context.SetOnRoot(key, 42);

        Assert.Multiple(() =>
        {
            Assert.That(root.Get<int>(key), Is.EqualTo(42), "should store value on root context");
            Assert.That(context.Get<int>(key), Is.EqualTo(42), "stored value should be readable in the writing context");
            Assert.That(fork.Get<int>(key), Is.EqualTo(42), "stored value should be visible to a forked context");
        });
    }
}