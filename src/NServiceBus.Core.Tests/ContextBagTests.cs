namespace NServiceBus.Core.Tests
{
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class ContextBagTests
    {
        [Test]
        public void ShouldAllowMonkeyPatching()
        {
            var contextBag = new ContextBag();

            contextBag.Set("MonkeyPatch", "some string");

            ((IReadOnlyContextBag)contextBag).TryGet("MonkeyPatch", out string theValue);
            Assert.AreEqual("some string", theValue);
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

            Assert.AreEqual(42, root.Get<int>(key), "should store value on root context");
            Assert.AreEqual(42, context.Get<int>(key), "stored value should be readable in the writing context");
            Assert.AreEqual(42, fork.Get<int>(key), "stored value should be visible to a forked context");
        }
    }
}