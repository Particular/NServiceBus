namespace NServiceBus.Core.Tests.Transports
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class QueueAddressTests
    {
        [TestCaseSource(nameof(queueAddresses))]
        public void ToStringTests((QueueAddress address, string expectedResult) test)
        {
            Assert.AreEqual(test.expectedResult, test.address.ToString());
        }

        static (QueueAddress, string)[] queueAddresses =
        {
            (new QueueAddress("BaseAddress"),
                "BaseAddress"),
            (new QueueAddress("BaseAddress", discriminator: "Discriminator"),
                "BaseAddress-Discriminator"),
            (new QueueAddress("BaseAddress", qualifier: "Qualifier"),
                "Qualifier.BaseAddress"),
            (new QueueAddress("BaseAddress", discriminator: "Discriminator", qualifier: "Qualifier"),
                "Qualifier.BaseAddress-Discriminator"),
            (new QueueAddress("BaseAddress", properties: new Dictionary<string, string> {{"key1", "value1"}, {"key2", "value2"}}),
                "BaseAddress(key1:value1;key2:value2)"),
            (new QueueAddress("BaseAddress", discriminator: "Discriminator", properties: new Dictionary<string, string> {{"key1", "value1"}, {"key2", "value2"}}),
                "BaseAddress-Discriminator(key1:value1;key2:value2)"),
            (new QueueAddress("BaseAddress", qualifier: "Qualifier", properties: new Dictionary<string, string> {{"key1", "value1"}, {"key2", "value2"}}),
                "Qualifier.BaseAddress(key1:value1;key2:value2)"),
            (new QueueAddress("BaseAddress", discriminator: "Discriminator", qualifier: "Qualifier", properties: new Dictionary<string, string> {{"key1", "value1"}, {"key2", "value2"}}),
                "Qualifier.BaseAddress-Discriminator(key1:value1;key2:value2)"),
        };


    }
}