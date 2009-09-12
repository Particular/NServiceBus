using System.Linq;
using NServiceBus.Host.Internal;
using NServiceBus.Host.Internal.ProfileHandlers;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class When_using_the_profile_manager
    {
        //private ProfileManager profileManager;

        // [SetUp]
        //public void SetUp()
        //{
        //    var assembliesToScan = new[] { typeof(ProductionProfileHandler).Assembly };

        //    profileManager = new ProfileManager(assembliesToScan, null);
 
        //}

        //[Test]
        //public void The_production_profile_should_be_used_as_default()
        //{
        //    var requestedProfile = new string[]{};

        //    var activeProfiles = profileManager.GetProfileConfigurationHandlersFor(requestedProfile);

        //    activeProfiles.First().ShouldBeInstanceOfType(typeof (ProductionProfileHandler));       
        //}

        //[Test]
        //public void Specific_profile_should_be_used_if_match_is_found()
        //{
        //    var requestedProfile = new[] { "integration" };

        //    var activeProfiles = profileManager.GetProfileConfigurationHandlersFor(requestedProfile);

        //    activeProfiles.First().ShouldBeInstanceOfType(typeof(IntegrationProfileHandler));
        //}


    }
}