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
            dict["test"] = "aString";

            message.Options[TestEnum.EnumValue1]["test"] = "aString";

            var result = (TestDto)mutator.MutateOutgoing(message);

            Assert.True(result.Options.ContainsKey(TestEnum.EnumValue1));
        }

        private enum TestEnum
        {
            EnumValue1
        }

        private class TestOptions
        {
            Dictionary<TestEnum, Dictionary<string, string>> dictionary = new Dictionary<TestEnum, Dictionary<string, string>>();
            public Dictionary<TestEnum, Dictionary<string, string>> Dictionary { get { return dictionary; } }

            public bool ContainsKey(TestEnum key)
            {
                return dictionary.ContainsKey(key);
            }

            public IEnumerable<TestEnum> Keys { get { return dictionary.Keys; } }

            public Dictionary<string, string> this[TestEnum appEnum]
            {
                get
                {
                    return dictionary.ContainsKey(appEnum)
                               ? dictionary[appEnum]
                               : dictionary[appEnum] = new Dictionary<string, string>();
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