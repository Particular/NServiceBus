namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class AssemblyPublisherSourceTests
    {
        [Test]
        public void It_returns_only_event_types()
        {
            var source = new AssemblyPublisherSource(Assembly.GetExecutingAssembly(), PublisherAddress.CreateFromEndpointName("Destination"));
            var routes = source.GenerateWithBestPracticeEnforcement(new Conventions()).ToArray();

            Assert.IsFalse(routes.Any(r => r.EventType == typeof(NonMessage)));
            Assert.IsFalse(routes.Any(r => r.EventType == typeof(NonEvent)));
        }


        [Test]
        public void It_throws_if_specified_assembly_contains_no_message_types()
        {
            var source = new AssemblyPublisherSource(typeof(string).Assembly, PublisherAddress.CreateFromEndpointName("Destination"));

            Assert.That(() => source.GenerateWithBestPracticeEnforcement(new Conventions()).ToArray(), Throws.Exception.Message.Contains("Cannot configure publisher for assembly"));
        }

        [Test]
        public void Without_best_practice_enforcement_it_throws_if_specified_assembly_contains_only_commands()
        {
            //Enforce artificial conventions to make sure the is a single command (but no messages) in an assembly
            var conventionBuilder = new ConventionsBuilder(new SettingsHolder());
            conventionBuilder.DefiningCommandsAs(t => t == typeof(string));


            var source = new AssemblyPublisherSource(typeof(string).Assembly, PublisherAddress.CreateFromEndpointName("Destination"));

            Assert.That(() => source.GenerateWithoutBestPracticeEnforcement(conventionBuilder.Conventions).ToArray(), Throws.Exception.Message.Contains("Cannot configure publisher for assembly"));
        }

        class NonMessage
        {
        }

        class NonEvent : IMessage
        {
        }
    }
}
