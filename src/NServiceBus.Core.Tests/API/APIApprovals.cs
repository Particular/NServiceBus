namespace NServiceBus.Core.Tests.API
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        public void ApproveNServiceBus()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "Particular.Licensing.ReleaseDateAttribute" });
            Approver.Verify(publicApi);
        }
    }
}