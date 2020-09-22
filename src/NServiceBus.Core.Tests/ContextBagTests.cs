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

            ((ReadOnlyContextBag) contextBag).TryGet("MonkeyPatch", out string theValue);
            Assert.AreEqual("some strsssing", theValue);
        }
    }
}