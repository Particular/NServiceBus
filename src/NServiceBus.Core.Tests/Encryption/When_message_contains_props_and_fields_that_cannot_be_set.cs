namespace NServiceBus.Core.Tests.Encryption
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class When_message_contains_props_and_fields_that_cannot_be_set : WireEncryptedStringContext
    {
        [Test]
        public void Should_ignore_those_properties_and_fields()
        {
            var message = new BogusEntityMessage{ Entity = new BogusEntity()};

            Assert.DoesNotThrow(() => mutator.MutateIncoming(message));
            Assert.DoesNotThrow(() => mutator.MutateOutgoing(message));
        }

        public class BogusEntityMessage : IMessage
        {
            public BogusEntity Entity { get; set; }
        }

        public class BogusEntity
        {
            //This field generates a stackoverflow
            string foo;

            public BogusEntity()
            {
                foo = "Foo";
            }

            public string ExposesReadOnlyField { get { return foo; } }

            //This property generates a stackoverflow
            public List<BogusEntity> ExposesGetOnlyProperty
            {
                get
                {
                    return new List<BogusEntity> { new BogusEntity() };
                }
            }
        }
    }
}