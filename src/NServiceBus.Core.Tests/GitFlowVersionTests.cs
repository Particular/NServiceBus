namespace NServiceBus
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class GitFlowVersionTests
    {
        [Test]
        public void Verify_has_a_version_and_can_be_parsed()
        {
            Assert.That(GitFlowVersion.MajorMinor, Is.Not.Null.Or.Empty);
            Version.Parse(GitFlowVersion.MajorMinor);
            Assert.That(GitFlowVersion.MajorMinorPatch, Is.Not.Null.Or.Empty);
            Version.Parse(GitFlowVersion.MajorMinorPatch);
        }
    }
}