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

        enum TestEnum
        {
            EnumValue1
        }

        class TestOptions
        {
            public Dictionary<TestEnum, Dictionary<string, string>> Dictionary { get; } = new Dictionary<TestEnum, Dictionary<string, string>>();

            public bool ContainsKey(TestEnum key)
            {
                return Dictionary.ContainsKey(key);
            }

            public IEnumerable<TestEnum> Keys => Dictionary.Keys;

            public Dictionary<string, string> this[TestEnum appEnum] => Dictionary.ContainsKey(appEnum)
                ? Dictionary[appEnum]
                : Dictionary[appEnum] = new Dictionary<string, string>();
        }

        class TestDto
        {
            public TestDto()
            {
                Options = new TestOptions();
            }

            public TestOptions Options { get; }
        }
    }
}