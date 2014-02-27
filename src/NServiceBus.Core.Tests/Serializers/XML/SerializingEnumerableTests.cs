﻿namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [Serializable]
    public class MessageWithEnumerableOfString
    {
        public Guid SagaId { get; set; }
        public IEnumerable<string> SomeStrings { get; set; }
    }

    [TestFixture]
    public class SerializingEnumerableTests
    {
        [Test]
        public void CanSerializeNullElements()
        {
            var message = new MessageWithEnumerableOfString
                {
                    SomeStrings = new[]
                        {
                            "element 1",
                            null,
                            null,
                           "element 2"
                        }
                };

            var result = ExecuteSerializer.ForMessage<MessageWithEnumerableOfString>(message);
            Assert.IsNotNull(result.SomeStrings);
            Assert.AreEqual(4, result.SomeStrings.Count());
        }
    }
}
