namespace NServiceBus.Core.Tests.Causation
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class AttachCausationHeadersBehaviorTests
    {
        [Test]
        public void Should_set_the_conversation_id_to_new_guid_when_not_sent_from_handler()
        {
            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            behavior.Invoke(context,()=>{});

            context.AssertHeaderWasSet(Headers.ConversationId,value=> value != Guid.Empty.ToString());
        }

        
        [Test]
        public void Should_set_the_conversation_id_to_conversation_id_of_incoming_message()
        {
            var incomingConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            var transportMessage = new TransportMessage("xyz", new Dictionary<string, string> { { Headers.ConversationId, incomingConversationId } });
            context.Set(transportMessage);

            behavior.Invoke(context, () => { });


            context.AssertHeaderWasSet(Headers.ConversationId, value => value == incomingConversationId);
        }

        [Test,Ignore("Will be refactored to use a explicit override via options instead and not rely on the header being set")]
        public void Should_not_override_a_conversation_id_specified_by_the_user()
        {
            var userConversationId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            
            behavior.Invoke(context, () => { });

            context.AssertHeaderWasSet(Headers.ConversationId, value => value == userConversationId);   
        }

        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            
            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            context.Set(new TransportMessage("the message id", new Dictionary<string, string>()));

            behavior.Invoke(context, () => { });

            context.AssertHeaderWasSet(Headers.RelatedTo, value => value == "the message id");   
        }

        static PhysicalOutgoingContextStageBehavior.Context InitializeContext()
        {
            var context = new PhysicalOutgoingContextStageBehavior.Context(null, ContextHelpers.GetOutgoingContext(new SendOptions()));
            return context;
        }

    
    }
}