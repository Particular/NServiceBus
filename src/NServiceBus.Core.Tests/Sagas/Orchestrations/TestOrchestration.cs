namespace NServiceBus.Core.Tests.Sagas.Orchestrations
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Sagas.Orchestrations;

    public class TestOrchestration : Orchestration<UserCreated>
    {
        static readonly TimeSpan CardIssuingDelayTime = TimeSpan.FromDays(1);

        protected override async Task Run(UserCreated input, IOrchestrationContext ctx)
        {
            var result = await ctx.Exec(new PerformCheckAntiMoneyLoundry { Email = input.Email });
            if (result.PositivelyVerified)
            {
                await ctx.Delay(CardIssuingDelayTime).ConfigureAwait(false);

                var cardId = ctx.NewGuid();

                var creditCard = await ctx.Exec(new IssueCreditCard
                {
                    CardId = cardId,
                    Name = input.Name,
                    InputSurname = input.Surname
                }).ConfigureAwait(false);

                await ctx.Exec(new AttachCard
                {
                    UserId = input.Id,
                    CardNumber = creditCard.CardNumber
                }).ConfigureAwait(false);
            }
        }
    }

    public class AttachCard : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public string CardNumber { get; set; }
    }

    public class IssueCreditCard : IRequest<CreditCardIssued>
    {
        public string InputSurname { get; set; }
        public string Name { get; set; }
        public Guid CardId { get; set; }
    }

    public class CreditCardIssued
    {
        public string CardNumber { get; set; }
    }

    public class AntiLoundryCheckPerformed
    {
        public bool PositivelyVerified { get; set; }
    }

    public class PerformCheckAntiMoneyLoundry : IRequest<AntiLoundryCheckPerformed>
    {
        public string Email { get; set; }
    }

    public class UserCreated : IEvent
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public Guid Id { get; set; }
    }
}