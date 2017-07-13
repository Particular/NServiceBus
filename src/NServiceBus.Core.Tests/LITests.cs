namespace NServiceBus.Core.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class LITests
    {
        [Test]
        public async Task name2()
        {
            var container1 = new CommonObjectBuilder(new LightInjectObjectBuilder());
            await Task.Yield();
            var container2 = new CommonObjectBuilder(new LightInjectObjectBuilder());

            var lazyPump1 = new Lazy<MyPump>(() => new MyPump());
            var lazyPump2 = new Lazy<MyPump>(() => new MyPump());

            await Task.Run(() =>
            {
                container1.ConfigureComponent(sf => (IMessagePump)lazyPump1.Value, DependencyLifecycle.InstancePerCall);
            });

            await Task.Run(() =>
            {
                container2.ConfigureComponent(sf => (IMessagePump)lazyPump2.Value, DependencyLifecycle.InstancePerCall);
            });

            await Task.WhenAll(
                Task.Run(() => container1.Build<IMessagePump>()),
                Task.Run(() => container1.Build<IMessagePump>()),
                Task.Run(() => container1.Build<IMessagePump>()));

            await Task.Run(() =>
            {
                var pump1 = container1.Build<IMessagePump>();
            });

            await Task.Run(() =>
            {
                var pump2 = container2.Build<IMessagePump>();
            });

        }
    }

    interface IMessagePump
    {
        
    }

    class SomeDependency
    {
        
    }

    class MyPump : IDisposable, IMessagePump
    {
        public void Dispose()
        {
        }
    }
}