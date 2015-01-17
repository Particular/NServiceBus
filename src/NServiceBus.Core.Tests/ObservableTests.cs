namespace NServiceBus.Core.Tests
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class ObservableTests
    {
        [Test]
        public void Observable_Can_Be_Disposed_Before_Subscription()
        {
            var observable = new Observable<Foo>();

            var subscription = observable.SubscribeOn(Scheduler.Default).Subscribe(
                f=> {});

            observable.Dispose();
            subscription.Dispose();
        }

        class Foo { }
    }
}