using System;
using RabbitMQ.Next.MessagePublisher.Abstractions.Transformers;

namespace RabbitMQ.Next.MessagePublisher.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class ApplicationIdAttribute : MessageAttributeBase
    {
        public ApplicationIdAttribute(string applicationId)
        {
            this.ApplicationId = applicationId;
        }

        public string ApplicationId { get; }

        public override void Apply(IMessageBuilder message)
        {
            if (string.IsNullOrEmpty(message.ApplicationId))
            {
                message.SetApplicationId(this.ApplicationId);
            }
        }
    }
}