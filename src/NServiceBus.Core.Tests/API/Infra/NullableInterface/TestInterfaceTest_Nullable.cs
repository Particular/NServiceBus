#nullable enable

namespace NServiceBus.Core.Tests.API.Infra.NullableInterface
{
    using NUnit.Framework;

    class TestInterfaceTest_Nullable
    {
        [Test]
        public void Test_with_no_nullable_operators()
        {
            var testInterface = new TestInterface();
            var message = "Hello, World!";
            var nullableMessage = (string)null;
            // Test WriteAMessage
            var result1 = testInterface.WriteAMessage(message);
            Assert.That(result1, Is.EqualTo(message));
            // Test WriteNullableMessage with non-null value
            var result2 = testInterface.WriteNullableMessage(message);
            Assert.That(result2, Is.EqualTo(message));
            // Test WriteNullableMessage with null value
            var result3 = testInterface.WriteNullableMessage(nullableMessage);
            Assert.That(result3, Is.EqualTo(nullableMessage));
            // Test ReturnNullableMessage with non-null value
            var result4 = testInterface.ReturnNullableMessage(message);
            Assert.That(result4, Is.EqualTo(message));
            // Test ReturnNullableMessage with null value
            var result5 = testInterface.ReturnNullableMessage(nullableMessage);
            Assert.That(result5, Is.EqualTo(nullableMessage));
        }
    }
}
