namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_resolving_message_handlers_for_a_saga
    {
        public class ValidMessage : IMessage
        {
            
        }

        public class SagaStartedByValidMessage : IHandleMessages<ValidMessage>
        {
            public void Handle(ValidMessage message)
            {
            }
        }
        
        public class InValidMessage 
        {
        }

        public class SagaStartedByInValidMessage : IHandleMessages<InValidMessage>
        {
            public void Handle(InValidMessage message)
            {
            }
        }

        [Test]
        public void It_should_return_the_interface_for_a_valid_message()
        {
            var messageTypes = NServiceBus.Features.Sagas.GetMessageTypesHandledBySaga(typeof(SagaStartedByValidMessage));
            Assert.AreEqual(1, messageTypes.Count());
        }
        [Test]
        public void It_should_throw_for_a_message_that_is_not_a_real_message()
        {
            var exception = Assert.Throws<Exception>(() => NServiceBus.Features.Sagas.GetMessageTypesHandledBySaga(typeof(SagaStartedByInValidMessage)).ToList());

            Assert.AreEqual("The saga 'NServiceBus.Core.Tests.Sagas.When_resolving_message_handlers_for_a_saga+SagaStartedByInValidMessage' implements 'IHandleMessages`1' but the message type 'NServiceBus.Core.Tests.Sagas.When_resolving_message_handlers_for_a_saga+InValidMessage' is not classified as a message. You should either use 'Unobtrusive Mode Messages' or the message should implement either 'IMessage', 'IEvent' or 'ICommand'.", exception.Message);
        }
    }
}