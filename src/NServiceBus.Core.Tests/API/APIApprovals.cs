namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
#if NET452
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveNServiceBus__NET452()
        {
            var publicApi = Filter(ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly));
            TestApprover.Verify(publicApi);
        }
#endif

#if NETCOREAPP2_0
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveNServiceBus__NETSTANDARD2_0()
        {
            var publicApi = Filter(ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly));
            TestApprover.Verify(publicApi);
        }
#endif

        string Filter(string text)
        {
            return string.Join(Environment.NewLine, text.Split(new[]
            {
                Environment.NewLine
            }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("[assembly: ReleaseDateAttribute("))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                );
        }
    }
}