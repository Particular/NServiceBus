namespace NServiceBus.PowerShell.Tests
{
    using System.Diagnostics;
    using NUnit.Framework;
    using Setup.Windows.Msmq;

    [TestFixture]
    public class MsmqSetupTests
    {
        [Explicit]
        [Test]
        public void IsMsmqInstalled()
        {
         Debug.WriteLine(MsmqSetup.IsMsmqInstalled());
        }
        [Explicit]
        [Test]
        public void IsInstallationGood()
        {
            Debug.WriteLine(MsmqSetup.IsInstallationGood());
        }

    }
}
