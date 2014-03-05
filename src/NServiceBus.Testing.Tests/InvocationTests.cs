namespace NServiceBus.Testing.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class InvocationTests
    {
        [Test]
        public void PublishBasicPositive()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => true};

            exp.Validate(i);
        }

        [Test]
        public void PublishValuePositive()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA {Value = 2}}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => m.Value == 2};

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void PublishBasicNegativeCheck()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => false};

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void PublishValueNegativeCheck()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA {Value = 2}}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => m.Value == 3};

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void PublishBasicNegativeType()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedPublishInvocation<MessageB> {Check = m => true};

            exp.Validate(i);
        }

        [Test]
        public void PublishBasicMultiplePositive()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var j = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => true};

            exp.Validate(i, j);
        }

        [Test]
        public void PublishValueMultiplePositive()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA {Value = 3}}};
            var j = new PublishInvocation<MessageA> {Messages = new[] {new MessageA {Value = 2}}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => m.Value == 2};

            exp.Validate(i, j);
        }

        [Test]
        [ExpectedException]
        public void NotPublishBasicNegative()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedNotPublishInvocation<MessageA> {Check = m => true};

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void SendPublishMismatchOne()
        {
            var i = new SendInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedPublishInvocation<MessageA> {Check = m => true};

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void SendPublishMismatchTwo()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var exp = new ExpectedSendInvocation<MessageA> {Check = m => true};

            exp.Validate(i);
        }

        [Test]
        public void OutOfOrderExecutionShouldSucceed()
        {
            var i = new PublishInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var j = new SendInvocation<MessageB> {Messages = new[] {new MessageB()}};
            var k = new SendLocalInvocation<MessageA> {Messages = new[] {new MessageA()}};
            var l = new HandleCurrentMessageLaterInvocation<object>();

            var exp1 = new ExpectedSendLocalInvocation<MessageA> {Check = m => true};
            var exp2 = new ExpectedSendInvocation<MessageB> {Check = m => true};
            var exp3 = new ExpectedPublishInvocation<MessageA> {Check = m => true};
            var exp4 = new ExpectedHandleCurrentMessageLaterInvocation<object>();

            exp1.Validate(i, j, k, l);
            exp2.Validate(i, j, k, l);
            exp3.Validate(i, j, k, l);
            exp4.Validate(i, j, k, l);
        }

        [Test]
        public void SendToSitesBasicPositive()
        {
            var i = new SendToSitesInvocation<MessageA> { Messages = new[] { new MessageA() }, Value = new[] {"SiteA"}};
            var exp = new ExpectedSendToSitesInvocation<MessageA> { Check = (m,a) => true };

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void SendToSitesBasicNegativeCheck()
        {
            var i = new SendToSitesInvocation<MessageA> { Messages = new[] { new MessageA() }, Value = new[] { "SiteA" } };
            var exp = new ExpectedSendToSitesInvocation<MessageA> { Check = (m, a) => false };

            exp.Validate(i);
        }

        [Test]
        [ExpectedException]
        public void NotSendToSitesBasicNegative()
        {
            var i = new SendToSitesInvocation<MessageA> { Messages = new[] { new MessageA() }, Value = new[] { "SiteA" } };
            var exp = new ExpectedNotSendToSitesInvocation<MessageA> { Check = (m, a) => true };

            exp.Validate(i);
        }

        [Test]
        public void NotSendToSitesBasicNegativeCheck()
        {
            var i = new SendToSitesInvocation<MessageA> { Messages = new[] { new MessageA() }, Value = new[] { "SiteA" } };
            var exp = new ExpectedNotSendToSitesInvocation<MessageA> { Check = (m, a) => false };

            exp.Validate(i);
        }

        [Test]
        public void DeferMessageBasicPositive()
        {
            var timespan = TimeSpan.FromMinutes(10);
            var i = new DeferMessageInvocation<MessageA, TimeSpan> { Messages = new object[] { new MessageA() }, Value = timespan };
            var exp = new ExpectedDeferMessageInvocation<MessageA, TimeSpan> { Check = (m, v) => v == timespan };

            exp.Validate(i);
        }

        [Test]
        public void DeferMessageBasicNegative()
        {
            var i = new DeferMessageInvocation<MessageA, TimeSpan> { Messages = new object[] { new MessageA() }, Value = TimeSpan.FromMinutes(10) };
            var exp = new ExpectedNotDeferMessageInvocation<MessageA, TimeSpan> { Check = (m, v) => false };

            exp.Validate(i);
        }
    }

    public class MessageA
    {
        public int Value { get; set; }
    }

    public class MessageB
    {
        public string Value { get; set; }
    }
}
