namespace NServiceBus.Core.Tests.API
{
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveNServiceBus()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly);
            TestApprover.Verify(publicApi);
        }
    }
}