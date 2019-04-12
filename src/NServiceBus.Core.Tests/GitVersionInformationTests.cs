namespace NServiceBus
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class GitVersionInformationTests
    {
        [Test]
        public void Verify_has_a_version_and_can_be_parsed()
        {
            Assert.That(GitVersionInformation.MajorMinorPatch, Is.Not.Null.Or.Empty);
            Version.Parse(GitVersionInformation.MajorMinorPatch);
        }
    }
}