namespace NServiceBus.Core.Tests.Audit
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
            var context = new PhysicalOutgoingContextStageBehavior.Context(new TransportMessage(),null);

            behavior.Invoke(context,()=>{});

            Assert.AreNotEqual(Guid.Empty,Guid.Parse(context.OutgoingMessage.Headers[Headers.ConversationId]));
        }

        [Test]
        public void Should_set_the_conversation_id_to_conversation_id_of_incoming_message()
        {
            var incomingConvId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = new PhysicalOutgoingContextStageBehavior.Context(new TransportMessage(),null);

            context.Set(TransportReceiveContext.IncomingPhysicalMessageKey,new TransportMessage("xyz",new Dictionary<string, string>{{Headers.ConversationId,incomingConvId}}));

            behavior.Invoke(context, () => { });

            Assert.AreEqual(incomingConvId, context.OutgoingMessage.Headers[Headers.ConversationId]);
        }

        [Test]
        public void Should_not_override_a_conversation_id_specified_by_the_user()
        {
            var userConvId = Guid.NewGuid().ToString();

            var behavior = new AttachCausationHeadersBehavior();
            var context = new PhysicalOutgoingContextStageBehavior.Context(new TransportMessage("xyz", 
                new Dictionary<string, string> { { Headers.ConversationId, userConvId } }), null);

            
            behavior.Invoke(context, () => { });

            Assert.AreEqual(userConvId, context.OutgoingMessage.Headers[Headers.ConversationId]);
            
        }

        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            
            var behavior = new AttachCausationHeadersBehavior();
            var context = new PhysicalOutgoingContextStageBehavior.Context(new TransportMessage(), null);

            context.Set(TransportReceiveContext.IncomingPhysicalMessageKey, new TransportMessage("the message id", new Dictionary<string, string>()));

            behavior.Invoke(context, () => { });

            Assert.AreEqual("the message id", context.OutgoingMessage.Headers[Headers.RelatedTo]);
        
        }
    }
}