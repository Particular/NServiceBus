namespace NServiceBus.Core.Tests.Audit
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
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
            var incomingConvId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            context.Set(TransportReceiveContext.IncomingPhysicalMessageKey,new TransportMessage("xyz",new Dictionary<string, string>{{Headers.ConversationId,incomingConvId}}));

            behavior.Invoke(context, () => { });


            context.AssertHeaderWasSet(Headers.ConversationId, value => value == incomingConvId);
        }

        [Test,Ignore("Will be refactored to use a explicit override via options instead and not rely on the header beeing set")]
        public void Should_not_override_a_conversation_id_specified_by_the_user()
        {
            var userConvId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            
            behavior.Invoke(context, () => { });

            context.AssertHeaderWasSet(Headers.ConversationId, value => value == userConvId);   
        }

        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            
            var behavior = new AttachCausationHeadersBehavior();
            var context = InitializeContext();

            context.Set(TransportReceiveContext.IncomingPhysicalMessageKey, new TransportMessage("the message id", new Dictionary<string, string>()));

            behavior.Invoke(context, () => { });

            context.AssertHeaderWasSet(Headers.RelatedTo, value => value == "the message id");   
        }

        static PhysicalOutgoingContextStageBehavior.Context InitializeContext()
        {
            var context = new PhysicalOutgoingContextStageBehavior.Context(null, new OutgoingContext(null, null, null, MessageIntentEnum.Send, null, null, new OptionExtensionContext()));
            return context;
        }

    
    }

    static class ContextAssertHelpers
    {
        public static void AssertHeaderWasSet(this PhysicalOutgoingContextStageBehavior.Context context, string key, Predicate<string> predicate)
        {
            var state = context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>();

            string current;

            if (state.Headers.TryGetValue(key, out current))
            {
                Assert.True(predicate(current),"Header {0} didn't have the expected value",key);
                return;
            }

            Assert.Fail("Header '{0}' was not set",key);
        } 
    }
}