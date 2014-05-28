namespace NServiceBus.Hosting.Tests
{
    using NUnit.Framework;
    using Roles;

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
            roleManager.ConfigureBusForEndpoint(new ConfigurationWithInheritedRole(),null);

            Assert.True(TestRoleConfigurer.ConfigureCalled);
        }

        [Test]
        public void Should_configure_requested_role()
        {
            roleManager.ConfigureBusForEndpoint(new ConfigurationWithTestRole(), null);

            Assert.True(TestRoleConfigurer.ConfigureCalled);
        }
    }

    class ConfigurationWithTestRole : IConfigureThisEndpoint, TestRole
    {
    }

    class ConfigurationWithInheritedRole : IConfigureThisEndpoint, IInheritedRole
    {
    }

    public interface TestRole : IRole
    {
    }

    public interface IInheritedRole : TestRole
    {
    }

    public class TestRoleConfigurer : IConfigureRole<TestRole>
    {
        public static bool ConfigureCalled;

        public void ConfigureRole(IConfigureThisEndpoint specifier,Configure config)
        {
            ConfigureCalled = true;
        }
    }
}