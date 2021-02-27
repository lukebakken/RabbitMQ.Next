using System;
using RabbitMQ.Next.Publisher.Abstractions.Transformers;

namespace RabbitMQ.Next.Publisher.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class ExchangeAttribute : MessageAttributeBase
    {
        public ExchangeAttribute(string exchange)
        {
            this.Exchange = exchange;
        }

        public string Exchange { get; }

        public override void Apply(IMessageBuilder message)
        {
            if (string.IsNullOrEmpty(message.Exchange))
            {
                message.SetExchange(this.Exchange);
            }
        }
    }
}