using RabbitMQ.Next.Abstractions.Messaging;

namespace RabbitMQ.Next.Publisher.Abstractions
{
    public readonly struct ReturnedMessage
    {
        public ReturnedMessage(string exchange, string routingKey, IMessageProperties properties, ushort replyCode, string replyText)
        {
            this.Exchange = exchange;
            this.RoutingKey = routingKey;
            this.Properties = properties;
            this.ReplyCode = replyCode;
            this.ReplyText = replyText;
        }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public IMessageProperties Properties { get; }

        public ushort ReplyCode { get; }

        public string ReplyText { get; }
    }
}