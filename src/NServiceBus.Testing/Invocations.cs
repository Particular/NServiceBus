namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    abstract class ActualInvocation { }

    interface IExpectedInvocation
    {
        void Validate(params ActualInvocation[] invocations);
    }

    abstract class ExpectedInvocation<T> : IExpectedInvocation where T : ActualInvocation
    {
        public void Validate(params ActualInvocation[] invocations)
        {
            var calls = invocations.Where(i => typeof(T) == i.GetType());
            var success = calls.Any(c =>
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

    class SingleMessageExpectedInvocation<INVOCATION, M> : ExpectedInvocation<INVOCATION> where INVOCATION : MessageInvocation<M>
    {
        public Func<M, bool> Check { get; set; }

        protected override bool Validate(INVOCATION invocation)
        {
            if (Check == null)
                return true;

            if (invocation.Message == null)
                return false;

            return Check((M)invocation.Message);
        }
    }

    class MessageInvocation<T> : ActualInvocation
    {
        public object Message { get; set; }
    }

    class SingleValueExpectedInvocation<INVOCATION, T> : ExpectedInvocation<INVOCATION> where INVOCATION : SingleValueInvocation<T>
    {
        public Func<T, bool> Check { get; set; }

        protected override bool Validate(INVOCATION invocation)
        {
            if (Check == null)
                return true;

            return Check(invocation.Value);
        }
    }

    class SingleValueInvocation<T> : ActualInvocation
    {
        public T Value { get; set; }
    }

    class ExpectedMessageAndValueInvocation<INVOCATION, M, K> : ExpectedInvocation<INVOCATION> where INVOCATION : MessageAndValueInvocation<M, K>
    {
        public Func<M, K, bool> Check { get; set; }

        protected override bool Validate(INVOCATION invocation)
        {
            if (Check == null)
                return true;

            if (invocation.Message == null)
                return false;

            return Check((M)invocation.Message, invocation.Value);
        }
    }

    class MessageAndValueInvocation<T, K> : MessageInvocation<T>
    {
        public K Value { get; set; }
    }

    class ExpectedPublishInvocation<M> : SingleMessageExpectedInvocation<PublishInvocation<M>, M> { }
    class PublishInvocation<M> : MessageInvocation<M> { }

    class ExpectedSendInvocation<M> : SingleMessageExpectedInvocation<SendInvocation<M>, M> { }
    class SendInvocation<M> : MessageInvocation<M> { }

    class ExpectedSendLocalInvocation<M> : SingleMessageExpectedInvocation<SendLocalInvocation<M>, M> { }
    class SendLocalInvocation<M> : MessageInvocation<M> { }

    class ExpectedReplyInvocation<M> : SingleMessageExpectedInvocation<ReplyInvocation<M>, M> { }
    class ReplyInvocation<M> : MessageInvocation<M> { }

    class ForwardCurrentMessageToInvocation : SingleValueInvocation<string> { }

    class ExpectedReturnInvocation<T> : SingleValueExpectedInvocation<ReturnInvocation<T>, T> { }
    class ReturnInvocation<T> : SingleValueInvocation<T> { }

    class ExpectedDeferMessageInvocation<M, D> : ExpectedMessageAndValueInvocation<DeferMessageInvocation<M, D>, M, D> { }
    class DeferMessageInvocation<M, D> : MessageAndValueInvocation<M, D> { }

    class ExpectedSendToDestinationInvocation<M> : ExpectedMessageAndValueInvocation<SendToDestinationInvocation<M>, M, string> { }

    class SendToDestinationInvocation<M> : MessageAndValueInvocation<M, string>
    {
        public string Address { get { return Value; } set { Value = value; } }
    }

    class ExpectedSendToSitesInvocation<M> : ExpectedMessageAndValueInvocation<SendToSitesInvocation<M>, M, IEnumerable<string>> { }
    class SendToSitesInvocation<M> : MessageAndValueInvocation<M, IEnumerable<string>> { }

    class ExpectedNotSendToSitesInvocation<M> : ExpectedSendToSitesInvocation<M>
    {
        public ExpectedNotSendToSitesInvocation()
        {
            Negate = true;
        }
    }

    //Slightly abusing the single message model as these don't actually care about the message type.
    class ExpectedHandleCurrentMessageLaterInvocation<M> : SingleMessageExpectedInvocation<HandleCurrentMessageLaterInvocation<M>, M> { }
    class HandleCurrentMessageLaterInvocation<M> : MessageInvocation<M> { }

    class ExpectedDoNotContinueDispatchingCurrentMessageToHandlersInvocation<M> : SingleMessageExpectedInvocation<DoNotContinueDispatchingCurrentMessageToHandlersInvocation<M>, M> { }
    class DoNotContinueDispatchingCurrentMessageToHandlersInvocation<M> : MessageInvocation<M> { }

    //other patterns
    class ExpectedNotPublishInvocation<M> : ExpectedPublishInvocation<M>
    {
        public ExpectedNotPublishInvocation()
        {
            Negate = true;
        }
    }

    class ExpectedNotSendInvocation<M> : ExpectedSendInvocation<M>
    {
        public ExpectedNotSendInvocation()
        {
            Negate = true;
        }
    }

    class ExpectedNotSendLocalInvocation<M> : ExpectedSendLocalInvocation<M>
    {
        public ExpectedNotSendLocalInvocation()
        {
            Negate = true;
        }
    }

    class ExpectedNotReplyInvocation<M> : ExpectedReplyInvocation<M>
    {
        public ExpectedNotReplyInvocation()
        {
            Negate = true;
        }
    }

    class ExpectedNotDeferMessageInvocation<M, D> : ExpectedDeferMessageInvocation<M, D>
    {
        public ExpectedNotDeferMessageInvocation()
        {
            Negate = true;
        }
    }

    class ExpectedReplyToOriginatorInvocation<M> : ExpectedInvocation<ReplyToOriginatorInvocation<M>>
    {
        public Func<M, string, string, bool> Check { get; set; }

        protected override bool Validate(ReplyToOriginatorInvocation<M> invocation)
        {
            if (Check == null)
                return true;

            if (invocation.Message == null )
                return false;

            return Check((M)invocation.Message, invocation.Address, invocation.CorrelationId);
        }
    }

    class ReplyToOriginatorInvocation<T> : MessageInvocation<T>
    {
        public string Address { get; set; }
        public string CorrelationId { get; set; }
    }
}
