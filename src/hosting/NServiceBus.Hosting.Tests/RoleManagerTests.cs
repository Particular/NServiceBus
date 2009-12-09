using System;
using System.Reflection;
using NBehave.Spec.NUnit;
using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;
using NUnit.Framework;

namespace NServiceBus.Hosting.Tests
{
    [TestFixture]
    public class RoleManagerTests
    {
        private RoleManager roleManager;

        [SetUp]
        public void SetUp()
        {
            roleManager = new RoleManager(new[] { typeof(RoleManagerTests).Assembly });
        }

        [Test]
        public void Should_configure_requested_role()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigurationWithTestRole());

            TestRoleConfigurer.ConfigureCalled.ShouldBeTrue();
        }

        [Test]
        public void Should_configure_inherited_roles()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigurationWithInheritedRole());

            TestRoleConfigurer.ConfigureCalled.ShouldBeTrue();
            
        }
    }

    internal class ConfigurationWithTestRole:IConfigureThisEndpoint,TestRole
    {
        
    }

    internal class ConfigurationWithInheritedRole : IConfigureThisEndpoint, InheritedRole
    {

    }

    public interface TestRole:IRole{}

    public interface InheritedRole : TestRole { }

    public class TestRoleConfigurer:IConfigureRole<TestRole>
    {
        public static bool ConfigureCalled = false;

        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            ConfigureCalled = true;

            return null;
        }
    }
}