namespace NServiceBus
{
    using Pipeline;

    class AddOrReplaceStep
    {
        public AddOrReplaceStep(RegisterStep registerStep, ReplaceStep replaceStep)
        {
            RegisterStep = registerStep;
            ReplaceStep = replaceStep;
        }

        public RegisterStep RegisterStep { get; }
        public ReplaceStep ReplaceStep { get; }
        public string StepId => ReplaceStep.ReplaceId;
    }
}