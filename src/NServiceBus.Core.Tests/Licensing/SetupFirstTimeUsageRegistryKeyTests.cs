namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using Microsoft.Win32;
    using NUnit.Framework;

    [TestFixture]
    public class SetupFirstTimeUsageRegistryKeyTests
    {
        [Test]
        public void Should_set_up_first_time_registry_keys()
        {
            var subKey = $@"Software\{Guid.NewGuid()}";
            try
            {
                // Able to create a key under HKCU\Software
                using (var regRoot = Registry.CurrentUser.CreateSubKey(subKey))
                {
                    // Get the value of NuGet user - should be false first.
                    Assert.IsNotNull(regRoot);
                    var nugetUser = regRoot.GetValue("NuGetUser");
                    Assert.IsNull(nugetUser);

                    // Set the value of NuGet user to true
                    regRoot.SetValue("NuGetUser", "true");
                    nugetUser = regRoot.GetValue("NuGetUser");
                    Assert.True(Convert.ToBoolean(nugetUser));
                }
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(subKey);
            }
        }
    }
}
