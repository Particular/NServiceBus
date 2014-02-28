﻿namespace NServiceBus
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class GitFlowVersionTests
    {
        [Test]
        public void Verify_has_a_version_and_can_be_parsed()
        {
            Assert.IsNotNullOrEmpty(GitFlowVersion.MajorMinor);
            Version.Parse(GitFlowVersion.MajorMinor);
            Assert.IsNotNullOrEmpty(GitFlowVersion.MajorMinorPatch);
            Version.Parse(GitFlowVersion.MajorMinorPatch);
        }
    }
}