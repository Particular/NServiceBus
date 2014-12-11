namespace NServiceBus.Hosting.Tests
{
    using NUnit.Framework;
    using Roles;
    using Unicast.Config;

    [TestFixture]
    public class RoleManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            roleManager = new RoleManager(new[] {typeof (RoleManagerTests).Assembly});
        }

        RoleManager roleManager;

        [Test]
        public void Should_configure_inherited_roles()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigurationWithInheritedRole());

            Assert.True(TestRoleConfigurer.ConfigureCalled);
        }

        [Test]
        public void Should_configure_requested_role()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigurationWithTestRole());

            Assert.True(TestRoleConfigurer.ConfigureCalled);
        }
    }

    internal class ConfigurationWithTestRole : IConfigureThisEndpoint, TestRole
    {
    }

    internal class ConfigurationWithInheritedRole : IConfigureThisEndpoint, InheritedRole
    {
    }

    public interface TestRole : IRole
    {
    }

    public interface InheritedRole : TestRole
    {
    }

    public class TestRoleConfigurer : IConfigureRole<TestRole>
    {
        public static bool ConfigureCalled = false;

        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            ConfigureCalled = true;

            return null;
        }
    }
}