namespace NServiceBus.Testing.Samples
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Testing.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class FlrBehaviorTest
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var flrStatusStorage = new FlrStatusStorage(0);
            var behavior = new FirstLevelRetriesBehavior(flrStatusStorage, new FirstLevelRetryPolicy(0));
            var context = Fake.CreateTransportReceiveContext();

            Func<Task> action = () => behavior.Invoke(context, () =>
            {
                throw new MessageDeserializationException("test");
            });

            Assert.Throws<MessageDeserializationException>(async () => await action(), "it should rethrow.");
            Assert.AreEqual(0, flrStatusStorage.StoredFailuresForMessage, "it should not increment the flr counter");
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var flrStatusStorage = new FlrStatusStorage(0);
            var behavior = new FirstLevelRetriesBehavior(flrStatusStorage, new FirstLevelRetryPolicy(1));
            var context = Fake.CreateTransportReceiveContext();

            Func<Task> action = () => behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            });

            Assert.Throws<MessageProcessingAbortedException>(async () => await action(), "it should throw MessageProcessingAbortedException");
            Assert.AreEqual(1, flrStatusStorage.StoredFailuresForMessage, "it should increment the flr counter");
        }

        [Test]
        public void ShouldSkipFLRWhenRetryCountExceedsRetryPolicy()
        {
            const int NumberOfRetries = 2;
            var flrStatusStorage = new FlrStatusStorage(NumberOfRetries);
            var behavior = new FirstLevelRetriesBehavior(flrStatusStorage, new FirstLevelRetryPolicy(NumberOfRetries));
            var context = Fake.CreateTransportReceiveContext();

            Func<Task> action = () => behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            });

            Assert.Throws<Exception>(async () => await action(), "it should rethrow the exception");
            Assert.AreEqual(NumberOfRetries.ToString(), context.Message.Headers[Headers.FLRetries], "it should set the FLRetries header on the message");
            Assert.AreEqual(0, flrStatusStorage.StoredFailuresForMessage, "it should reset the FLR counter for this message");
        }
    }

#region testee
    class FirstLevelRetriesBehavior : Behavior<TransportReceiveContext>
    {
        public FirstLevelRetriesBehavior(FlrStatusStorage storage, FirstLevelRetryPolicy retryPolicy)
        {
            this.storage = storage;
            this.retryPolicy = retryPolicy;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageDeserializationException)
            {
                throw; // no retries for poison messages
            }
            catch (Exception ex)
            {
                var messageId = context.Message.MessageId;
                var pipelineUniqueMessageId = context.PipelineInfo.Name + messageId;

                var numberOfFailures = storage.GetFailuresForMessage(pipelineUniqueMessageId);

                if (retryPolicy.ShouldGiveUp(numberOfFailures))
                {
                    storage.ClearFailuresForMessage(pipelineUniqueMessageId);
                    context.Message.Headers[Headers.FLRetries] = numberOfFailures.ToString();
                    //notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.Message, ex);
                    Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
                    throw;
                }

                storage.IncrementFailuresForMessage(pipelineUniqueMessageId);

                Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", ex);
                
                //notifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(numberOfFailures, context.Message, ex);

                throw new MessageProcessingAbortedException();
            }
        }

        FlrStatusStorage storage;
        FirstLevelRetryPolicy retryPolicy;

        static ILog Logger = LogManager.GetLogger<FirstLevelRetriesBehavior>();
    }

    public class MessageProcessingAbortedException : Exception
    {
    }

    public class FirstLevelRetryPolicy
    {
        int retryTreshhold;

        public FirstLevelRetryPolicy(int retryTreshhold)
        {
            this.retryTreshhold = retryTreshhold;
        }

        public bool ShouldGiveUp(int numberOfFailures)
        {
            return numberOfFailures >= retryTreshhold;
        }
    }

    public class FlrStatusStorage
    {
        public int StoredFailuresForMessage { get; private set; }

        public FlrStatusStorage(int storedFailuresForMessage)
        {
            this.StoredFailuresForMessage = storedFailuresForMessage;
        }

        public int GetFailuresForMessage(string pipelineUniqueMessageId)
        {
            return StoredFailuresForMessage;
        }

        public void ClearFailuresForMessage(string pipelineUniqueMessageId)
        {
            StoredFailuresForMessage = 0;
        }

        public void IncrementFailuresForMessage(string pipelineUniqueMessageId)
        {
            StoredFailuresForMessage++;
        }
    }
#endregion
}