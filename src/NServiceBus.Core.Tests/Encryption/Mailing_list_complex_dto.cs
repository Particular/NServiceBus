namespace NServiceBus.Core.Tests.Encryption
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class Mailing_list_complex_dto : WireEncryptedStringContext
    {
        [Test]
        public void Indexed_enum_property()
        {
            var message = new TestDto();

            var dict = message.Options[TestEnum.EnumValue1];
            dict["test"] = "asdf";

            message.Options[TestEnum.EnumValue1]["test"] = "asdf";

            var result = (TestDto)mutator.MutateOutgoing(message);

            Assert.True(result.Options.ContainsKey(TestEnum.EnumValue1));
        }

        private enum TestEnum
        {
            EnumValue1
        }

        private class TestOptions
        {
            private readonly Dictionary<TestEnum, Dictionary<string, string>> _dictionary = new Dictionary<TestEnum, Dictionary<string, string>>();
            public Dictionary<TestEnum, Dictionary<string, string>> Dictionary { get { return _dictionary; } }

            public bool ContainsKey(TestEnum key)
            {
                return _dictionary.ContainsKey(key);
            }

            public IEnumerable<TestEnum> Keys { get { return _dictionary.Keys; } }

            public Dictionary<string, string> this[TestEnum appEnum]
            {
                get
                {
                    return _dictionary.ContainsKey(appEnum)
                               ? _dictionary[appEnum]
                               : _dictionary[appEnum] = new Dictionary<string, string>();
                }
            }
        }

        private class TestDto
        {
            public TestDto()
            {
                Options = new TestOptions();
            }

            public TestOptions Options { get; set; }
        }
    }
}