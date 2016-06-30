namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_scope_dispose_throws : ScopeTransportTest
    {
        [Test]
        public async Task Should_call_on_error()
        {
            var onErrorTsc = new TaskCompletionSource<bool>();

            await StartPump(c =>
            {
                onErrorTsc.SetResult(true);
                //todo enlist and throw in dispose
                //new FakeEnlistment();
                return Task.FromResult(0);
            },
                c =>
                {
                    onErrorTsc.SetResult(true);
                    return Task.FromResult(false);
                });

            await SendMessage(InputQueueName);

            var done = await onErrorTsc.Task;

            Assert.True(done);
        }
    }
}