namespace NServiceBus
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Pipeline;

    class EncryptBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public EncryptBehavior(EncryptionInspector messageInspector, IEncryptionService encryptionService)
        {
            this.messageInspector = messageInspector;
            this.encryptionService = encryptionService;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            var currentMessageToSend = context.Message.Instance;

            foreach (var item in messageInspector.ScanObject(currentMessageToSend))
            {
                EncryptMember(item.Item1, item.Item2, context);
            }

            context.UpdateMessage(currentMessageToSend);

            return next();
        }

        void EncryptMember(object message, MemberInfo member, IOutgoingLogicalMessageContext context)
        {
            var valueToEncrypt = member.GetValue(message);

            var wireEncryptedString = valueToEncrypt as WireEncryptedString;
            if (wireEncryptedString != null)
            {
                encryptionService.EncryptValue(wireEncryptedString, context);
                return;
            }

            var stringToEncrypt = valueToEncrypt as string;
            if (stringToEncrypt != null)
            {
                encryptionService.EncryptValue(ref stringToEncrypt, context);

                member.SetValue(message, stringToEncrypt);
                return;
            }

            throw new Exception("Only string properties is supported for convention based encryption, check the configured conventions.");
        }

        IEncryptionService encryptionService;
        EncryptionInspector messageInspector;

        public class EncryptRegistration : RegisterStep
        {
            public EncryptRegistration(EncryptionInspector inspector, IEncryptionService encryptionService)
                : base("InvokeEncryption", typeof(EncryptBehavior), "Invokes the encryption logic", b => new EncryptBehavior(inspector, encryptionService))
            {
                InsertAfter("MutateOutgoingMessages");
            }
        }
    }
}