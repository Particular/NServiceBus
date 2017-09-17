#if NET452
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
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApproveNServiceBus()
        {
            var publicApi = Filter(ApiGenerator.GeneratePublicApi(typeof(Endpoint).Assembly));
            TestApprover.Verify(publicApi);
        }

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
#endif