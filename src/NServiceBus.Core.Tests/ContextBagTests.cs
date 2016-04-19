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

            string theValue;

            ((ReadOnlyContextBag) contextBag).TryGet("MonkeyPatch", out theValue);
            Assert.AreEqual("some string", theValue);
        }
    }
}