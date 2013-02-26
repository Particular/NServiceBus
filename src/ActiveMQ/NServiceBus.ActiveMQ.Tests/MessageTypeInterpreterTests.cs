namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using FluentAssertions;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class MessageTypeInterpreterTests
    {
        private MessageTypeInterpreter testee;

        [SetUp]
        public void SetUp()
        {
            this.testee = new MessageTypeInterpreter();
        }        

        [Test]
        public void GetAssemblyQualifiedName_IfNull_ReturnsEmptyString()
        {
            var result = this.testee.GetAssemblyQualifiedName(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void GetAssemblyQualifiedName_IfEmptyString_ReturnsEmptyString()
        {
            var result = this.testee.GetAssemblyQualifiedName(string.Empty);

            result.Should().BeEmpty();
        }

        [Test]
        public void GetAssemblyQualifiedName_IfAssemblyQualifiedName_ReturnsAssemblyQualifiedName()
        {
            var name = this.GetType().AssemblyQualifiedName;

            var result = this.testee.GetAssemblyQualifiedName(name);

            result.Should().Be(name);
        }

        [Test]
        public void GetAssemblyQualifiedName_IfFullName_ReturnsAssemblyQualifiedName()
        {
            var name = this.GetType().FullName;
            var expectedName = this.GetType().AssemblyQualifiedName;

            var result = this.testee.GetAssemblyQualifiedName(name);

            result.Should().Be(expectedName);
        }

        [Test]
        public void GetAssemblyQualifiedName_IfUninterpretableType_ReturnsInputValue()
        {
            const string Name = "SomeNonsenseType";

            var result = this.testee.GetAssemblyQualifiedName(Name);

            result.Should().Be(Name);
        }
    }
}