namespace NServiceBus.Core.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class LITests
    {
        [Test]
        public async Task failing1()
        {
            CommonObjectBuilder container1 = null;

            await Task.Run(() =>
            {
                container1 = new CommonObjectBuilder(new LightInjectObjectBuilder());
            });

            var lazyPump1 = new Lazy<MyPump>(() => new MyPump());
            container1.ConfigureComponent(sf => (IMessagePump)lazyPump1.Value, DependencyLifecycle.InstancePerCall);

            var x = container1.Build<IMessagePump>();
        }

        [Test]
        public async Task failing2()
        {
            var container1 = await CreateBuilder();

            var lazyPump1 = new Lazy<MyPump>(() => new MyPump());
            container1.ConfigureComponent(sf => (IMessagePump)lazyPump1.Value, DependencyLifecycle.InstancePerCall);

            var x = container1.Build<IMessagePump>();
        }

        [Test]
        public async Task failing3()
        {
            var container1 = await CreateBuilder();

            container1.ConfigureComponent<MyPump>(DependencyLifecycle.InstancePerCall);

            var x = container1.Build<IMessagePump>();
        }

#pragma warning disable CS1998
        async Task<CommonObjectBuilder> CreateBuilder()
        {
            //await Task.Yield();
            return new CommonObjectBuilder(new LightInjectObjectBuilder());
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