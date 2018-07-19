namespace NServiceBus.Core.Tests.API
{
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
#if NETFRAMEWORK
        [Test]
        public void ApproveNServiceBus()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly, excludeAttributes: new[] { "Particular.Licensing.ReleaseDateAttribute" });
            Approver.Verify(publicApi, scenario: "netframework");
        }
#endif

#if NETCOREAPP
        [Test]
        public void ApproveNServiceBus()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly, excludeAttributes: new[] { "Particular.Licensing.ReleaseDateAttribute" });
            Approver.Verify(publicApi, scenario: "netstandard");
        }
#endif
    }
}