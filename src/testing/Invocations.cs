using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Testing
{
    public abstract class ActualInvocation { }

    public interface IExpectedInvocation
    {
        void Validate(params ActualInvocation[] invocations);
    }

    public abstract class ExpectedInvocation<T> : IExpectedInvocation where T : ActualInvocation
    {
        public void Validate(params ActualInvocation[] invocations)
        {
            var calls = invocations.Where(i => typeof(T) == i.GetType());
            bool success = calls.Any(c =>
                                         {
                                             var result = Validate(c as T);
                                             
                                             return result;
                                         });

            if ((!success && !Negate) || (Negate && success))
                throw new Exception(string.Format("{0} not fulfilled.\nCalls made:\n{1}", filter(GetType()), string.Join("\n", invocations.Select(i => filter(i.GetType())))));
        }

        protected abstract bool Validate(T invocation);
        protected bool Negate;

        private readonly Func<Type, string> filter =
                    t =>
                    {
                        var s = t.ToString().Replace("NServiceBus.Testing.", "").Replace("`1[", "<").Replace("`2[",
                                                                                                             "<");
                        if (s.EndsWith("]"))
                        {
                            s = s.Substring(0, s.Length - 1);
                            s += ">";
                        }

                        return s;
                    };

    }

    public class SingleMessageExpectedInvocation<INVOCATION, M> : ExpectedInvocation<INVOCATION> where INVOCATION : MessageInvocation<M>
    {
        public Func<M, bool> Check { get; set; }

        protected override bool Validate(INVOCATION invocation)
        {
            if (Check == null)
                return true;

            if (invocation.Messages == null || invocation.Messages.Length != 1)
                return false;

            return Check((M)invocation.Messages[0]);
        }
    }

    public class MessageInvocation<T> : ActualInvocation
    {
        public object[] Messages { get; set; }
    }

    public class SingleValueExpectedInvocation<INVOCATION, T> : ExpectedInvocation<INVOCATION> where INVOCATION : SingleValueInvocation<T>
    {
        public Func<T, bool> Check { get; set; }

        protected override bool Validate(INVOCATION invocation)
        {
            if (Check == null)
                return true;

            return Check(invocation.Value);
        }
    }

    public class SingleValueInvocation<T> : ActualInvocation
    {
        public T Value { get; set; }
    }

    public class ExpectedMessageAndValueInvocation<INVOCATION, M, K> : ExpectedInvocation<INVOCATION> where INVOCATION : MessageAndValueInvocation<M, K>
    {
        public Func<M, K, bool> Check { get; set; }

        protected override bool Validate(INVOCATION invocation)
        {
            if (Check == null)
                return true;

            if (invocation.Messages == null || invocation.Messages.Length != 1)
                return false;

            return Check((M)invocation.Messages[0], invocation.Value);
        }
    }

    public class MessageAndValueInvocation<T, K> : MessageInvocation<T>
    {
        public K Value { get; set; }
    }

    public class ExpectedPublishInvocation<M> : SingleMessageExpectedInvocation<PublishInvocation<M>, M> { }
    public class PublishInvocation<M> : MessageInvocation<M> { }

    public class ExpectedSendInvocation<M> : SingleMessageExpectedInvocation<SendInvocation<M>, M> { }
    public class SendInvocation<M> : MessageInvocation<M> { }

    public class ExpectedSendLocalInvocation<M> : SingleMessageExpectedInvocation<SendLocalInvocation<M>, M> { }
    public class SendLocalInvocation<M> : MessageInvocation<M> { }

    public class ExpectedReplyInvocation<M> : SingleMessageExpectedInvocation<ReplyInvocation<M>, M> { }
    public class ReplyInvocation<M> : MessageInvocation<M> { }

    public class ExpectedForwardCurrentMessageToInvocation : SingleValueExpectedInvocation<ForwardCurrentMessageToInvocation, string> { }
    public class ForwardCurrentMessageToInvocation : SingleValueInvocation<string> { }

    public class ExpectedReturnInvocation<T> : SingleValueExpectedInvocation<ReturnInvocation<T>, T> { }
    public class ReturnInvocation<T> : SingleValueInvocation<T> { }

    public class ExpectedDeferMessageInvocation<M, D> : ExpectedMessageAndValueInvocation<DeferMessageInvocation<M, D>, M, D> { }
    public class DeferMessageInvocation<M, D> : MessageAndValueInvocation<M, D> { }

    public class ExpectedSendToDestinationInvocation<M> : ExpectedMessageAndValueInvocation<SendToDestinationInvocation<M>, M, Address> { }

    public class SendToDestinationInvocation<M> : MessageAndValueInvocation<M, Address>
    {
        public Address Address { get { return Value; } set { Value = value; } }
    }

    public class ExpectedSendToSitesInvocation<M> : ExpectedMessageAndValueInvocation<SendToSitesInvocation<M>, M, IEnumerable<string>> { }
    public class SendToSitesInvocation<M> : MessageAndValueInvocation<M, IEnumerable<string>> { }

    public class ExpectedNotSendToSitesInvocation<M> : ExpectedSendToSitesInvocation<M>
    {
        public ExpectedNotSendToSitesInvocation()
        {
            Negate = true;
        }
    }

    //Slightly abusing the single message model as these don't actually care about the message type.
    public class ExpectedHandleCurrentMessageLaterInvocation<M> : SingleMessageExpectedInvocation<HandleCurrentMessageLaterInvocation<M>, M> { }
    public class HandleCurrentMessageLaterInvocation<M> : MessageInvocation<M> { }

    public class ExpectedDoNotContinueDispatchingCurrentMessageToHandlersInvocation<M> : SingleMessageExpectedInvocation<DoNotContinueDispatchingCurrentMessageToHandlersInvocation<M>, M> { }
    public class DoNotContinueDispatchingCurrentMessageToHandlersInvocation<M> : MessageInvocation<M> { }

    //other patterns
    public class ExpectedNotPublishInvocation<M> : ExpectedPublishInvocation<M>
    {
        public ExpectedNotPublishInvocation()
        {
            Negate = true;
        }
    }

    public class ExpectedNotSendInvocation<M> : ExpectedSendInvocation<M>
    {
        public ExpectedNotSendInvocation()
        {
            Negate = true;
        }
    }

    public class ExpectedNotSendLocalInvocation<M> : ExpectedSendLocalInvocation<M>
    {
        public ExpectedNotSendLocalInvocation()
        {
            Negate = true;
        }
    }

    public class ExpectedNotReplyInvocation<M> : ExpectedReplyInvocation<M>
    {
        public ExpectedNotReplyInvocation()
        {
            Negate = true;
        }
    }

    public class ExpectedReplyToOriginatorInvocation<M> : ExpectedInvocation<ReplyToOriginatorInvocation<M>>
    {
        public Func<M, Address, string, bool> Check { get; set; }

        protected override bool Validate(ReplyToOriginatorInvocation<M> invocation)
        {
            if (Check == null)
                return true;

            if (invocation.Messages == null || invocation.Messages.Length != 1)
                return false;

            return Check((M)invocation.Messages[0], invocation.Address, invocation.CorrelationId);
        }
    }

    public class ReplyToOriginatorInvocation<T> : MessageInvocation<T>
    {
        public Address Address { get; set; }
        public string CorrelationId { get; set; }
    }
}
