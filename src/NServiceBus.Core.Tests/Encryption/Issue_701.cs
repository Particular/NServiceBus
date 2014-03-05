﻿namespace NServiceBus.Core.Tests.Encryption
{
    using NUnit.Framework;

    [TestFixture]
    public class Issue_701 : WireEncryptedStringContext
    {
        [Test]
        public void No_get_on_property()
        {
            var message = new TestMessageWithSets
            {
                Name = "John"
            };

            var result = (TestMessageWithSets)mutator.MutateOutgoing(message);

            Assert.AreEqual("John", result.Name);
        }

        [Test]
        public void No_set_on_property()
        {
            var message = new TestMessageWithGets
            {
                Name = "John"
            };

            var result = (TestMessageWithGets)mutator.MutateOutgoing(message);

            Assert.AreEqual("John", result.Name);
        }

        private class TestMessageWithSets
        {
            public string Name { get; set; }

            public string Options1
            {
// ReSharper disable once ValueParameterNotUsed
                set
                {
                    //do nothing
                }
            }

            public int Options2
            {
// ReSharper disable once ValueParameterNotUsed
                set
                {
                    //do nothing
                }
            }
        }

        private class TestMessageWithGets
        {
            public string Name { get; set; }

            public string Options1
            {
                get { return "Testing"; }
            }

            public int Options2
            {
                get { return 5; }
            }
        }
    }
}